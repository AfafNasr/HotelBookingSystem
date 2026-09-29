using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.Api.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Bookings;

public sealed class GetMyBookingsIntegrationTests
{
    private const string Password = "Password123!";

    private const string GetMyBookingsUrl =
        "/api/bookings";

    // ============================================================
    // 1. Authentication boundary
    // ============================================================

    [Fact]
    public async Task GetMyBookings_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                GetMyBookingsUrl);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ============================================================
    // 2. Authenticated user with no bookings
    // ============================================================

    [Fact]
    public async Task GetMyBookings_WhenUserHasNoBookings_ShouldReturnEmptyCollection()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        // Act
        var response =
            await client.GetAsync(
                GetMyBookingsUrl);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var bookings =
            await response.Content
                .ReadFromJsonAsync<GetMyBookingResponse[]>();

        Assert.NotNull(bookings);

        Assert.Empty(bookings);
    }

    // ============================================================
    // 3. User isolation
    // ============================================================

    [Fact]
    public async Task GetMyBookings_WhenTwoCustomersHaveBookings_ShouldReturnOnlyCurrentUsersBookings()
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

        var firstScenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var secondScenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var firstCheckInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var firstCheckOutDate =
            firstCheckInDate.AddDays(3);

        var secondCheckInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(20));

        var secondCheckOutDate =
            secondCheckInDate.AddDays(2);

        // --------------------------------------------------------
        // First customer creates first booking
        // --------------------------------------------------------

        await AuthenticateAsync(
            client,
            firstCustomer.UserName!,
            Password);

        var firstBooking =
            await CreateBookingAsync(
                client,
                firstScenario,
                firstCheckInDate,
                firstCheckOutDate,
                "First Customer",
                "first.customer@example.com",
                "+970599000001");

        // --------------------------------------------------------
        // Second customer creates second booking
        // --------------------------------------------------------

        await AuthenticateAsync(
            client,
            secondCustomer.UserName!,
            Password);

        var secondBooking =
            await CreateBookingAsync(
                client,
                secondScenario,
                secondCheckInDate,
                secondCheckOutDate,
                "Second Customer",
                "second.customer@example.com",
                "+970599000002");

        Assert.NotEqual(
            firstBooking.BookingId,
            secondBooking.BookingId);

        // --------------------------------------------------------
        // Login again as first customer
        // --------------------------------------------------------

        await AuthenticateAsync(
            client,
            firstCustomer.UserName!,
            Password);

        // Act
        var response =
            await client.GetAsync(
                GetMyBookingsUrl);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert - HTTP response
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var bookings =
            await response.Content
                .ReadFromJsonAsync<GetMyBookingResponse[]>();

        Assert.NotNull(bookings);

        // The first customer created exactly one booking.
        Assert.Single(bookings);

        var booking =
            bookings.Single();

        // Verify first customer's booking.
        Assert.Equal(
            firstBooking.BookingId,
            booking.BookingId);

        Assert.Equal(
            firstCheckInDate,
            booking.CheckInDate);

        Assert.Equal(
            firstCheckOutDate,
            booking.CheckOutDate);

        Assert.Equal(
            firstScenario.PricePerNight * 3,
            booking.TotalAmount);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.Null(
            booking.ConfirmationNumber);

        Assert.False(
            string.IsNullOrWhiteSpace(
                booking.HotelName));

        Assert.True(
            booking.CreatedAt > DateTime.MinValue);

        // Critical security assertion:
        // another user's booking must not leak.
        Assert.DoesNotContain(
            bookings,
            item =>
                item.BookingId ==
                secondBooking.BookingId);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static async Task<CreateBookingResponse>
        CreateBookingAsync(
            HttpClient client,
            BookingScenario scenario,
            DateOnly checkInDate,
            DateOnly checkOutDate,
            string guestFullName,
            string guestEmail,
            string guestPhoneNumber)
    {
        var request =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                checkInDate,
                checkOutDate,
                guestFullName,
                guestEmail,
                guestPhoneNumber,
                null);

        var response =
            await client.PostAsJsonAsync(
                "/api/bookings",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Booking setup failed. " +
            $"Expected 201 Created but received " +
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
         * We reuse one HttpClient in the isolation test.
         * Clear the previous identity before logging in as another user.
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