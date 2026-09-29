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

public sealed class CancelBookingIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. HAPPY PATH
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenPendingBookingBelongsToCurrentUser_ShouldCancelBooking()
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

        // Act
        var response =
            await client.PostAsync(
                $"/api/bookings/{booking.BookingId}/cancel",
                content: null);

        var responseBody =
            await response.Content.ReadAsStringAsync();

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
            BookingStatus.Cancelled,
            persistedBooking.Status);

        Assert.Null(
            persistedBooking.ExpiresAt);

        Assert.NotNull(
            persistedBooking.UpdatedAt);
    }

    // ============================================================
    // 2. AUTHENTICATION BOUNDARY
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        // Act
        var response =
            await client.PostAsync(
                "/api/bookings/1/cancel",
                content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ============================================================
    // 3. OWNERSHIP / SECURITY
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenBookingBelongsToAnotherUser_ShouldReturnForbiddenAndNotModifyBooking()
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

        // Create the booking as its real owner.
        await AuthenticateAsync(
            client,
            bookingOwner.UserName!,
            Password);

        var booking =
            await CreateBookingAsync(
                client,
                scenario);

        // Switch identity to another customer.
        await AuthenticateAsync(
            client,
            otherCustomer.UserName!,
            Password);

        // Act
        var response =
            await client.PostAsync(
                $"/api/bookings/{booking.BookingId}/cancel",
                content: null);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected 403 Forbidden but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        // Assert - Database must remain unchanged.
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
            BookingStatus.PendingPayment,
            persistedBooking.Status);

        Assert.NotNull(
            persistedBooking.ExpiresAt);
    }

    // ============================================================
    // 4. INVALID STATE
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenBookingIsConfirmed_ShouldReturnConflictAndKeepBookingConfirmed()
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

        // Act
        var response =
            await client.PostAsync(
                $"/api/bookings/{booking.BookingId}/cancel",
                content: null);

        var responseBody =
            await response.Content.ReadAsStringAsync();

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

        Assert.NotNull(
            persistedBooking.ConfirmationNumber);
    }

    // ============================================================
    // 5. IDEMPOTENCY
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenCalledTwice_ShouldRemainSuccessfulAndCancelled()
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

        var url =
            $"/api/bookings/{booking.BookingId}/cancel";

        // Act
        var firstResponse =
            await client.PostAsync(
                url,
                content: null);

        var secondResponse =
            await client.PostAsync(
                url,
                content: null);

        // Assert - HTTP
        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            secondResponse.StatusCode);

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
            BookingStatus.Cancelled,
            persistedBooking.Status);

        Assert.Null(
            persistedBooking.ExpiresAt);
    }

    // ============================================================
    // 6. CANCELLED BOOKING RELEASES ROOM
    // ============================================================

    [Fact]
    public async Task CancelBooking_WhenBookingIsCancelled_ShouldReleaseRoomForAnotherBooking()
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

        var firstCustomer =
            await CreateUserAsync(
                factory,
                $"customer-one-{unique}",
                $"customer-one-{unique}@test.com",
                Roles.Customer);

        var secondCustomer =
            await CreateUserAsync(
                factory,
                $"customer-two-{unique}",
                $"customer-two-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        // First customer books the room.
        await AuthenticateAsync(
            client,
            firstCustomer.UserName!,
            Password);

        var firstBooking =
            await CreateBookingAsync(
                client,
                scenario,
                checkInDate,
                checkOutDate);

        // First customer cancels the booking.
        var cancelResponse =
            await client.PostAsync(
                $"/api/bookings/{firstBooking.BookingId}/cancel",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            cancelResponse.StatusCode);

        // Second customer tries to reserve the same physical room
        // for exactly the same dates.
        await AuthenticateAsync(
            client,
            secondCustomer.UserName!,
            Password);

        var secondBookingRequest =
            CreateBookingRequest(
                scenario,
                checkInDate,
                checkOutDate,
                "Second Customer",
                "second.customer@example.com",
                "+970599000099");

        // Act
        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/bookings",
                secondBookingRequest);

        var responseBody =
            await secondResponse.Content
                .ReadAsStringAsync();

        // Assert
        Assert.True(
            secondResponse.StatusCode == HttpStatusCode.Created,
            $"Expected the room to become available after cancellation, " +
            $"but received {(int)secondResponse.StatusCode} " +
            $"{secondResponse.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var secondBooking =
            await secondResponse.Content
                .ReadFromJsonAsync<CreateBookingResponse>();

        Assert.NotNull(secondBooking);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookings =
            await dbContext.Bookings
                .AsNoTracking()
                .Where(
                    item =>
                        item.Id == firstBooking.BookingId ||
                        item.Id == secondBooking.BookingId)
                .ToListAsync();

        Assert.Equal(
            2,
            bookings.Count);

        Assert.Contains(
            bookings,
            item =>
                item.Id == firstBooking.BookingId &&
                item.Status == BookingStatus.Cancelled);

        Assert.Contains(
            bookings,
            item =>
                item.Id == secondBooking.BookingId &&
                item.Status == BookingStatus.PendingPayment);
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

        return await CreateBookingAsync(
            client,
            scenario,
            checkInDate,
            checkOutDate);
    }

    private static async Task<CreateBookingResponse>
        CreateBookingAsync(
            HttpClient client,
            BookingScenario scenario,
            DateOnly checkInDate,
            DateOnly checkOutDate)
    {
        var request =
            CreateBookingRequest(
                scenario,
                checkInDate,
                checkOutDate,
                "Integration Customer",
                "integration.customer@test.com",
                "+970599000000");

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

    private static CreateBookingRequest CreateBookingRequest(
        BookingScenario scenario,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        string guestFullName,
        string guestEmail,
        string guestPhoneNumber)
    {
        return new CreateBookingRequest(
            scenario.HotelId,
            [scenario.RoomId],
            checkInDate,
            checkOutDate,
            guestFullName,
            guestEmail,
            guestPhoneNumber,
            null);
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

        var now =
            DateTime.UtcNow;

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.NotNull(
            booking.ExpiresAt);

        /*
         * This test only needs a valid Confirmed booking state
         * to verify that cancellation rejects that transition.
         *
         * Payment confirmation itself already has its own
         * integration coverage.
         */
        booking.Confirm(
            $"CONF-{Guid.NewGuid():N}",
            now);

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
                .GetRequiredService<UserManager<IdentityUser>>();

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
         * Some tests switch between multiple customers using
         * the same HttpClient, so remove the previous token first.
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