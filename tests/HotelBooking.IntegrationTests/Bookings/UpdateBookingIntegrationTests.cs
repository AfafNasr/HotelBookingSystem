using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.Api.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Bookings;

public sealed class UpdateBookingIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. HAPPY PATH
    // ============================================================

    [Fact]
    public async Task UpdateBooking_WhenPendingBookingBelongsToCurrentUser_ShouldUpdateGuestDetails()
    {
        // Arrange
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

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var booking =
            await CreateBookingAsync(
                client,
                scenario);

        var request =
            new UpdateBookingRequest(
                "Updated Integration Guest",
                "updated.integration@test.com",
                "+970599111111",
                "Updated special request");

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/bookings/{booking.BookingId}",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 204 NoContent but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        // Assert - Database
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var persistedBooking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == booking.BookingId);

        Assert.Equal(
            "Updated Integration Guest",
            persistedBooking.GuestFullName);

        Assert.Equal(
            "updated.integration@test.com",
            persistedBooking.GuestEmail);

        Assert.Equal(
            "+970599111111",
            persistedBooking.GuestPhoneNumber);

        Assert.Equal(
            "Updated special request",
            persistedBooking.SpecialRequests);

        Assert.Equal(
            BookingStatus.PendingPayment,
            persistedBooking.Status);

        Assert.NotNull(
            persistedBooking.ExpiresAt);

        Assert.NotNull(
            persistedBooking.UpdatedAt);
    }

    // ============================================================
    // 2. AUTHENTICATION
    // ============================================================

    [Fact]
    public async Task UpdateBooking_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var request =
            new UpdateBookingRequest(
                "Updated Guest",
                "updated@test.com",
                "+970599111111",
                null);

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/bookings/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ============================================================
    // 3. OWNERSHIP / SECURITY
    // ============================================================

    [Fact]
    public async Task UpdateBooking_WhenBookingBelongsToAnotherUser_ShouldReturnForbiddenAndNotModifyBooking()
    {
        // Arrange
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
                $"other-customer-{unique}",
                $"other-customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        await AuthenticateAsync(
            client,
            bookingOwner.UserName!,
            Password);

        var booking =
            await CreateBookingAsync(
                client,
                scenario);

        await AuthenticateAsync(
            client,
            otherCustomer.UserName!,
            Password);

        var request =
            new UpdateBookingRequest(
                "Attacker Update",
                "attacker@test.com",
                "+970599999999",
                "Should never be saved");

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/bookings/{booking.BookingId}",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected 403 Forbidden but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        // Assert - Database must remain unchanged
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var persistedBooking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == booking.BookingId);

        Assert.Equal(
            "Integration Customer",
            persistedBooking.GuestFullName);

        Assert.Equal(
            "integration.customer@test.com",
            persistedBooking.GuestEmail);

        Assert.Equal(
            "+970599000000",
            persistedBooking.GuestPhoneNumber);

        Assert.Equal(
            BookingStatus.PendingPayment,
            persistedBooking.Status);
    }

    // ============================================================
    // 4. INVALID STATE
    // ============================================================

    [Fact]
    public async Task UpdateBooking_WhenBookingIsConfirmed_ShouldReturnConflictAndNotModifyGuestDetails()
    {
        // Arrange
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

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var booking =
            await CreateBookingAsync(
                client,
                scenario);

        await ConfirmBookingAsync(
            factory,
            booking.BookingId);

        var request =
            new UpdateBookingRequest(
                "Should Not Change",
                "should.not.change@test.com",
                "+970599999999",
                "Should not be persisted");

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/bookings/{booking.BookingId}",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409 Conflict but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        // Assert - Database
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var persistedBooking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == booking.BookingId);

        Assert.Equal(
            BookingStatus.Confirmed,
            persistedBooking.Status);

        Assert.Equal(
            "Integration Customer",
            persistedBooking.GuestFullName);

        Assert.Equal(
            "integration.customer@test.com",
            persistedBooking.GuestEmail);

        Assert.Equal(
            "+970599000000",
            persistedBooking.GuestPhoneNumber);

        Assert.NotNull(
            persistedBooking.ConfirmationNumber);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static async Task<CreateBookingResponse>
        CreateBookingAsync(
            HttpClient client,
            BookingScenario scenario)
    {
        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var request =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                checkInDate,
                checkOutDate,
                "Integration Customer",
                "integration.customer@test.com",
                "+970599000000",
                "Original special request");

        var response =
            await client.PostAsJsonAsync(
                "/api/bookings",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Booking setup failed. Expected 201 Created but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateBookingResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.BookingId > 0);

        Assert.Equal(
            BookingStatus.PendingPayment,
            result.Status);

        return result;
    }

    private static async Task ConfirmBookingAsync(
        CustomWebApplicationFactory factory,
        int bookingId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var booking =
            await dbContext.Bookings
                .SingleAsync(
                    item =>
                        item.Id == bookingId);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.NotNull(
            booking.ExpiresAt);

        booking.Confirm(
            $"CONF-{Guid.NewGuid():N}",
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync();
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
        /*
         * Some tests change the authenticated user on the same
         * HttpClient, so remove any old bearer token first.
         */
        client.DefaultRequestHeaders.Authorization =
            null;

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    username,
                    password));

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

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
}