using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.Api.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Bookings;

public sealed class GetBookingConfirmationIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task GetBookingConfirmation_WhenBookingIsReady_ShouldReturnConfirmation()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreateConfirmedBookingWithSuccessfulPaymentAsync(
                factory,
                customer.Id,
                scenario);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var response =
            await client.GetAsync(
                $"/api/bookings/{setup.BookingId}/confirmation");

        var responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var result =
            await response.Content
                .ReadFromJsonAsync<GetBookingConfirmationResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            setup.ConfirmationNumber,
            result.ConfirmationNumber);

        Assert.Equal(
            "Review Test Customer",
            result.GuestFullName);

        Assert.Equal(
            "review.customer@test.com",
            result.GuestEmail);

        Assert.Equal(
            setup.CheckInDate,
            result.CheckInDate);

        Assert.Equal(
            setup.CheckOutDate,
            result.CheckOutDate);

        Assert.Equal(
            3,
            result.NumberOfNights);

        Assert.Single(
            result.Rooms);

        var room =
            result.Rooms.Single();

        Assert.Equal(
            scenario.RoomId,
            room.RoomId);

        Assert.Equal(
            scenario.PricePerNight,
            room.OriginalPricePerNight);

        Assert.Equal(
            scenario.PricePerNight * 3,
            result.SubtotalAmount);

        Assert.Equal(
            0m,
            result.DiscountAmount);

        Assert.Equal(
            scenario.PricePerNight * 3,
            result.TotalAmount);

        Assert.Equal(
            PaymentStatus.Succeeded,
            result.PaymentStatus);

        Assert.Equal(
            scenario.PricePerNight * 3,
            result.PaymentAmount);

        Assert.Equal(
            "usd",
            result.Currency);
    }

    [Fact]
    public async Task GetBookingConfirmation_WhenBookingBelongsToAnotherUser_ShouldReturnNotFound()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var bookingOwner =
            await CreateUserAsync(
                factory,
                $"booking-owner-{unique}",
                $"booking-owner-{unique}@test.com",
                Roles.Customer);

        var otherCustomer =
            await CreateUserAsync(
                factory,
                $"other-{unique}",
                $"other-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreateConfirmedBookingWithSuccessfulPaymentAsync(
                factory,
                bookingOwner.Id,
                scenario);

        await AuthenticateAsync(
            client,
            otherCustomer.UserName!,
            Password);

        var response =
            await client.GetAsync(
                $"/api/bookings/{setup.BookingId}/confirmation");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetBookingConfirmation_WhenPaymentIsStillPending_ShouldReturnConflict()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateConfirmedBookingWithPendingPaymentAsync(
                factory,
                customer.Id,
                scenario);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var response =
            await client.GetAsync(
                $"/api/bookings/{bookingId}/confirmation");

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task GetBookingConfirmation_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync(
                "/api/bookings/1/confirmation");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task<ConfirmationSetup>
        CreateConfirmedBookingWithSuccessfulPaymentAsync(
            CustomWebApplicationFactory factory,
            string userId,
            BookingScenario scenario)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkInDate =
            DateOnly.FromDateTime(
                now.AddDays(5));

        var checkOutDate =
            checkInDate.AddDays(3);

        var totalAmount =
            scenario.PricePerNight * 3;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Review Test Customer",
                "review.customer@test.com",
                "+970599000000",
                null,
                totalAmount,
                now);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        var confirmationNumber =
            $"CONF-{Guid.NewGuid():N}";

        booking.Confirm(
            confirmationNumber,
            now.AddMinutes(1));

        var payment =
            new Payment(
                booking.Id,
                totalAmount,
                "USD",
                now);

        payment.AttachProviderPaymentIntent(
            $"pi_{Guid.NewGuid():N}",
            now.AddMinutes(1));

        payment.MarkSucceeded(
            now.AddMinutes(2));

        dbContext.Payments.Add(
            payment);

        await dbContext.SaveChangesAsync();

        return new ConfirmationSetup(
            booking.Id,
            confirmationNumber,
            checkInDate,
            checkOutDate);
    }

    private static async Task<int>
        CreateConfirmedBookingWithPendingPaymentAsync(
            CustomWebApplicationFactory factory,
            string userId,
            BookingScenario scenario)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkInDate =
            DateOnly.FromDateTime(
                now.AddDays(5));

        var checkOutDate =
            checkInDate.AddDays(3);

        var totalAmount =
            scenario.PricePerNight * 3;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Pending Payment Customer",
                "pending.customer@test.com",
                "+970599000000",
                null,
                totalAmount,
                now);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        booking.Confirm(
            $"CONF-{Guid.NewGuid():N}",
            now.AddMinutes(1));

        var payment =
            new Payment(
                booking.Id,
                totalAmount,
                "USD",
                now);

        payment.AttachProviderPaymentIntent(
            $"pi_{Guid.NewGuid():N}",
            now.AddMinutes(1));

        dbContext.Payments.Add(
            payment);

        await dbContext.SaveChangesAsync();

        return booking.Id;
    }

    private static async Task<IdentityUser>
        CreateUserAsync(
            CustomWebApplicationFactory factory,
            string username,
            string email,
            string role)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<IdentityUser>>();

        var user =
            new IdentityUser
            {
                UserName = username,
                Email = email
            };

        var createResult =
            await userManager.CreateAsync(
                user,
                Password);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                ", ",
                createResult.Errors.Select(
                    error =>
                        error.Description)));

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        Assert.True(
            roleResult.Succeeded,
            string.Join(
                ", ",
                roleResult.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string username,
        string password)
    {
        client.DefaultRequestHeaders.Authorization =
            null;

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    username,
                    password));

        var responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected login to return 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var loginResult =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginResult.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }

    private sealed record ConfirmationSetup(
        int BookingId,
        string ConfirmationNumber,
        DateOnly CheckInDate,
        DateOnly CheckOutDate);
}