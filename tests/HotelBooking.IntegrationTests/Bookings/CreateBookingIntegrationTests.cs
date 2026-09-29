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

public sealed class CreateBookingIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. HAPPY PATH
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenRequestIsValid_ShouldCreatePendingBooking()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner = await CreateUserAsync(
            factory,
            $"owner-{unique}",
            $"owner-{unique}@test.com",
            Roles.HotelOwner);

        var customer = await CreateUserAsync(
            factory,
            $"customer-{unique}",
            $"customer-{unique}@test.com",
            Roles.Customer);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var request = new CreateBookingRequest(
            scenario.HotelId,
            [scenario.RoomId],
            checkInDate,
            checkOutDate,
            "Integration Customer",
            "guest@example.com",
            "+970599000000",
            "Late check-in");

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/bookings",
                request);

        // Assert - HTTP
        var responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateBookingResponse>();

        Assert.NotNull(result);

        Assert.True(result.BookingId > 0);

        Assert.Equal(
            BookingStatus.PendingPayment,
            result.Status);

        // Assert - Database
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var booking =
            await dbContext.Bookings
                .AsNoTracking()
                .Include(booking => booking.Rooms)
                .SingleAsync(
                    booking =>
                        booking.Id == result.BookingId);

        Assert.Equal(
            customer.Id,
            booking.UserId);

        Assert.Equal(
            scenario.HotelId,
            booking.HotelId);

        Assert.Equal(
            checkInDate,
            booking.CheckInDate);

        Assert.Equal(
            checkOutDate,
            booking.CheckOutDate);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.NotNull(
            booking.ExpiresAt);

        Assert.Equal(
            "Integration Customer",
            booking.GuestFullName);

        Assert.Equal(
            "guest@example.com",
            booking.GuestEmail);

        Assert.Equal(
            "+970599000000",
            booking.GuestPhoneNumber);

        Assert.Equal(
            "Late check-in",
            booking.SpecialRequests);

        Assert.Single(
            booking.Rooms);

        var bookingRoom =
            booking.Rooms.Single();

        Assert.Equal(
            scenario.RoomId,
            bookingRoom.RoomId);

        Assert.Equal(
            scenario.PricePerNight,
            bookingRoom.OriginalPricePerNight);

        var expectedTotal =
            scenario.PricePerNight * 3;

        Assert.Equal(
            expectedTotal,
            booking.TotalAmount);
    }

    // ============================================================
    // 2. AUTHENTICATION BOUNDARY
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenUserIsUnauthenticated_ShouldReturnUnauthorizedAndNotPersistBooking()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner = await CreateUserAsync(
            factory,
            $"owner-{unique}",
            $"owner-{unique}@test.com",
            Roles.HotelOwner);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var request = new CreateBookingRequest(
            scenario.HotelId,
            [scenario.RoomId],
            checkInDate,
            checkOutDate,
            "Anonymous Guest",
            "anonymous@example.com",
            "+970599000001",
            null);

        // Act
        // Intentionally NOT authenticated.
        var response =
            await client.PostAsJsonAsync(
                "/api/bookings",
                request);

        // Assert - HTTP
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        // Assert - Database was not modified
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookingExists =
            await dbContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    booking =>
                        booking.HotelId == scenario.HotelId);

        Assert.False(bookingExists);
    }

    // ============================================================
    // 3. ROOM AVAILABILITY / OVERLAP
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenRoomHasOverlappingActiveBooking_ShouldReturnConflictAndNotCreateSecondBooking()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner = await CreateUserAsync(
            factory,
            $"owner-{unique}",
            $"owner-{unique}@test.com",
            Roles.HotelOwner);

        var firstCustomer = await CreateUserAsync(
            factory,
            $"customer-one-{unique}",
            $"customer-one-{unique}@test.com",
            Roles.Customer);

        var secondCustomer = await CreateUserAsync(
            factory,
            $"customer-two-{unique}",
            $"customer-two-{unique}@test.com",
            Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var firstCheckInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var firstCheckOutDate =
            firstCheckInDate.AddDays(4);

        // First customer books the room.
        await AuthenticateAsync(
            client,
            firstCustomer.UserName!,
            Password);

        var firstRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                firstCheckInDate,
                firstCheckOutDate,
                "First Customer",
                "first.customer@example.com",
                "+970599000010",
                null);

        var firstResponse =
            await client.PostAsJsonAsync(
                "/api/bookings",
                firstRequest);

        var firstResponseBody =
            await firstResponse.Content
                .ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode ==
            HttpStatusCode.Created,
            $"The setup booking should have been created, " +
            $"but received {(int)firstResponse.StatusCode} " +
            $"{firstResponse.StatusCode}." +
            $"{Environment.NewLine}" +
            firstResponseBody);

        var firstResult =
            await firstResponse.Content
                .ReadFromJsonAsync<CreateBookingResponse>();

        Assert.NotNull(firstResult);

        // Use another customer for the competing booking.
        await AuthenticateAsync(
            client,
            secondCustomer.UserName!,
            Password);

        /*
         * Existing booking:
         *
         * Day 10 ---------------- Day 14
         *
         * Second request:
         *
         *          Day 12 ---------------- Day 15
         *
         * They overlap, so the same physical room
         * must not be booked again.
         */
        var secondCheckInDate =
            firstCheckInDate.AddDays(2);

        var secondCheckOutDate =
            firstCheckOutDate.AddDays(1);

        var secondRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                secondCheckInDate,
                secondCheckOutDate,
                "Second Customer",
                "second.customer@example.com",
                "+970599000011",
                null);

        // Act
        var secondResponse =
            await client.PostAsJsonAsync(
                "/api/bookings",
                secondRequest);

        var secondResponseBody =
            await secondResponse.Content
                .ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            secondResponse.StatusCode ==
            HttpStatusCode.Conflict,
            $"Expected 409 Conflict for an overlapping room booking " +
            $"but received {(int)secondResponse.StatusCode} " +
            $"{secondResponse.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            secondResponseBody);

        // Assert - only the original booking exists
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookings =
            await dbContext.Bookings
                .AsNoTracking()
                .Where(
                    booking =>
                        booking.HotelId == scenario.HotelId)
                .ToListAsync();

        Assert.Single(bookings);

        Assert.Equal(
            firstResult.BookingId,
            bookings[0].Id);

        Assert.Equal(
            firstCustomer.Id,
            bookings[0].UserId);

        Assert.Equal(
            BookingStatus.PendingPayment,
            bookings[0].Status);
    }

    // ============================================================
    // 4. ROOM / HOTEL CONSISTENCY
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenRoomBelongsToDifferentHotel_ShouldReturnBadRequestAndNotPersistBooking()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var firstOwner = await CreateUserAsync(
            factory,
            $"owner-one-{unique}",
            $"owner-one-{unique}@test.com",
            Roles.HotelOwner);

        var secondOwner = await CreateUserAsync(
            factory,
            $"owner-two-{unique}",
            $"owner-two-{unique}@test.com",
            Roles.HotelOwner);

        var customer = await CreateUserAsync(
            factory,
            $"customer-{unique}",
            $"customer-{unique}@test.com",
            Roles.Customer);

        var firstHotel =
            await BookingTestData.CreateAsync(
                factory.Services,
                firstOwner);

        var secondHotel =
            await BookingTestData.CreateAsync(
                factory.Services,
                secondOwner);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        /*
         * HotelId belongs to firstHotel,
         * but RoomId belongs to secondHotel.
         */
        var request =
            new CreateBookingRequest(
                firstHotel.HotelId,
                [secondHotel.RoomId],
                checkInDate,
                checkOutDate,
                "Integration Customer",
                "guest@example.com",
                "+970599000020",
                null);

        // Act
        var response =
            await client.PostAsJsonAsync(
                "/api/bookings",
                request);

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode ==
            HttpStatusCode.BadRequest,
            $"Expected 400 BadRequest when the room belongs " +
            $"to another hotel but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        // Assert - Database was not modified
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookingExists =
            await dbContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    booking =>
                        booking.HotelId ==
                        firstHotel.HotelId);

        Assert.False(bookingExists);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static async Task<IdentityUser> CreateUserAsync(
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

        var user = new IdentityUser
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
            response.StatusCode ==
            HttpStatusCode.OK,
            $"Expected login to return 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
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