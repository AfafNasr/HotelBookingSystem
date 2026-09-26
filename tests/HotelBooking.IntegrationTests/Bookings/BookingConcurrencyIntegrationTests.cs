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

public sealed class BookingConcurrencyIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Same room + same dates
    //    Exactly one request must win.
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenTwoUsersBookSameRoomAtSameTime_ShouldAllowOnlyOneBooking()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var firstClient =
            factory.CreateClient();

        using var secondClient =
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

        await AuthenticateAsync(
            firstClient,
            firstCustomer.UserName!,
            Password);

        await AuthenticateAsync(
            secondClient,
            secondCustomer.UserName!,
            Password);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var firstRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                checkInDate,
                checkOutDate,
                "First Customer",
                "first.customer@test.com",
                "+970599000001",
                null);

        var secondRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                checkInDate,
                checkOutDate,
                "Second Customer",
                "second.customer@test.com",
                "+970599000002",
                null);

        /*
         * Both tasks wait on the same gate.
         *
         * This gives us a real race:
         * neither request is intentionally allowed to finish
         * before the other starts.
         */
        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask =
            SendBookingWhenReleasedAsync(
                firstClient,
                firstRequest,
                startGate.Task);

        var secondTask =
            SendBookingWhenReleasedAsync(
                secondClient,
                secondRequest,
                startGate.Task);

        // Act
        startGate.SetResult();

        var responses =
            await Task.WhenAll(
                firstTask,
                secondTask);

        // Assert - HTTP outcomes
        var statusCodes =
            responses
                .Select(response => response.StatusCode)
                .ToArray();

        Assert.Equal(
            1,
            statusCodes.Count(
                status =>
                    status == HttpStatusCode.Created));

        Assert.Equal(
            1,
            statusCodes.Count(
                status =>
                    status == HttpStatusCode.Conflict));

        // Assert - database invariant
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookings =
            await dbContext.Bookings
                .AsNoTracking()
                .Include(booking => booking.Rooms)
                .Where(booking =>
                    booking.HotelId == scenario.HotelId &&
                    booking.CheckInDate == checkInDate &&
                    booking.CheckOutDate == checkOutDate)
                .ToListAsync();

        /*
         * Critical invariant:
         *
         * Two concurrent requests must NEVER create
         * two active bookings for the same room/date range.
         */
        Assert.Single(bookings);

        var persistedBooking =
            bookings.Single();

        Assert.Equal(
            BookingStatus.PendingPayment,
            persistedBooking.Status);

        Assert.Single(
            persistedBooking.Rooms);

        Assert.Equal(
            scenario.RoomId,
            persistedBooking.Rooms.Single().RoomId);
    }

    // ============================================================
    // 2. Same room + non-overlapping dates
    //    Both requests should succeed.
    // ============================================================

    [Fact]
    public async Task CreateBooking_WhenTwoUsersBookSameRoomForNonOverlappingDatesAtSameTime_ShouldCreateBothBookings()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var firstClient =
            factory.CreateClient();

        using var secondClient =
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

        await AuthenticateAsync(
            firstClient,
            firstCustomer.UserName!,
            Password);

        await AuthenticateAsync(
            secondClient,
            secondCustomer.UserName!,
            Password);

        var firstCheckInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var firstCheckOutDate =
            firstCheckInDate.AddDays(3);

        /*
         * Second booking starts exactly when
         * the first booking ends.
         *
         * [10, 13)
         * [13, 15)
         *
         * No overlap.
         */
        var secondCheckInDate =
            firstCheckOutDate;

        var secondCheckOutDate =
            secondCheckInDate.AddDays(2);

        var firstRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                firstCheckInDate,
                firstCheckOutDate,
                "First Customer",
                "first.customer@test.com",
                "+970599000001",
                null);

        var secondRequest =
            new CreateBookingRequest(
                scenario.HotelId,
                [scenario.RoomId],
                secondCheckInDate,
                secondCheckOutDate,
                "Second Customer",
                "second.customer@test.com",
                "+970599000002",
                null);

        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask =
            SendBookingWhenReleasedAsync(
                firstClient,
                firstRequest,
                startGate.Task);

        var secondTask =
            SendBookingWhenReleasedAsync(
                secondClient,
                secondRequest,
                startGate.Task);

        // Act
        startGate.SetResult();

        var responses =
            await Task.WhenAll(
                firstTask,
                secondTask);

        // Assert - both requests succeeded
        Assert.All(
            responses,
            response =>
                Assert.Equal(
                    HttpStatusCode.Created,
                    response.StatusCode));

        // Assert - database contains both bookings
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookings =
            await dbContext.Bookings
                .AsNoTracking()
                .Include(booking => booking.Rooms)
                .Where(booking =>
                    booking.HotelId == scenario.HotelId)
                .ToListAsync();

        Assert.Equal(
            2,
            bookings.Count);

        Assert.Contains(
            bookings,
            booking =>
                booking.CheckInDate == firstCheckInDate &&
                booking.CheckOutDate == firstCheckOutDate);

        Assert.Contains(
            bookings,
            booking =>
                booking.CheckInDate == secondCheckInDate &&
                booking.CheckOutDate == secondCheckOutDate);

        Assert.All(
            bookings,
            booking =>
            {
                Assert.Equal(
                    BookingStatus.PendingPayment,
                    booking.Status);

                Assert.Single(
                    booking.Rooms);

                Assert.Equal(
                    scenario.RoomId,
                    booking.Rooms.Single().RoomId);
            });
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static async Task<HttpResponseMessage>
        SendBookingWhenReleasedAsync(
            HttpClient client,
            CreateBookingRequest request,
            Task startSignal)
    {
        await startSignal;

        return await client.PostAsJsonAsync(
            "/api/bookings",
            request);
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