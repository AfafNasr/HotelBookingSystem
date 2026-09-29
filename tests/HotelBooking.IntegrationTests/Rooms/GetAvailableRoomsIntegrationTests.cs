using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Rooms;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Rooms;

public sealed class GetAvailableRoomsIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Room without bookings should be available
    // ============================================================

    [Fact]
    public async Task GetAvailableRooms_WhenRoomHasNoBookings_ShouldReturnRoom()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
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

        var query =
            new GetAvailableRoomsQuery(
                scenario.HotelId,
                RoomType.Standard,
                checkInDate,
                checkOutDate,
                2,
                1);

        // Act
        var result =
            await ExecuteQueryAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Single(result.Rooms);

        var room =
            result.Rooms.Single();

        Assert.Equal(
            scenario.RoomId,
            room.Id);

        Assert.Equal(
            RoomType.Standard,
            room.RoomType);

        Assert.Equal(
            2,
            room.AdultsCapacity);

        Assert.Equal(
            1,
            room.ChildrenCapacity);

        Assert.Equal(
            scenario.PricePerNight,
            room.PricePerNight);
    }

    // ============================================================
    // 2. Active overlapping PendingPayment booking blocks room
    // ============================================================

    [Fact]
    public async Task GetAvailableRooms_WhenRoomHasOverlappingActivePendingBooking_ShouldExcludeRoom()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

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

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        await AddPendingBookingAsync(
            factory,
            customer.Id,
            scenario.HotelId,
            scenario.RoomId,
            scenario.PricePerNight,
            checkInDate,
            checkOutDate);

        var query =
            new GetAvailableRoomsQuery(
                scenario.HotelId,
                RoomType.Standard,
                checkInDate,
                checkOutDate,
                2,
                1);

        // Act
        var result =
            await ExecuteQueryAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.DoesNotContain(
            result.Rooms,
            room =>
                room.Id == scenario.RoomId);
    }

    // ============================================================
    // 3. Non-overlapping booking should NOT block room
    // ============================================================

    [Fact]
    public async Task GetAvailableRooms_WhenExistingBookingDoesNotOverlap_ShouldReturnRoom()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

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

        var bookingCheckInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var bookingCheckOutDate =
            bookingCheckInDate.AddDays(3);

        await AddPendingBookingAsync(
            factory,
            customer.Id,
            scenario.HotelId,
            scenario.RoomId,
            scenario.PricePerNight,
            bookingCheckInDate,
            bookingCheckOutDate);

        /*
         * Existing booking:
         * [Day 10, Day 13)
         *
         * Search:
         * [Day 13, Day 15)
         *
         * They touch at the boundary,
         * but they do not overlap.
         */
        var searchCheckInDate =
            bookingCheckOutDate;

        var searchCheckOutDate =
            searchCheckInDate.AddDays(2);

        var query =
            new GetAvailableRoomsQuery(
                scenario.HotelId,
                RoomType.Standard,
                searchCheckInDate,
                searchCheckOutDate,
                2,
                1);

        // Act
        var result =
            await ExecuteQueryAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Contains(
            result.Rooms,
            room =>
                room.Id == scenario.RoomId);
    }

    // ============================================================
    // 4. Room type and capacity filtering
    // ============================================================

    [Fact]
    public async Task GetAvailableRooms_ShouldFilterByRoomTypeAndCapacity()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        /*
         * Keep RoomNumber short because the DB column
         * has a limited maximum length.
         */
        var shortId =
            Guid.NewGuid()
                .ToString("N")[..6];

        var matchingRoomId =
            await AddRoomAsync(
                factory,
                scenario.HotelId,
                $"STD-{shortId}",
                RoomType.Standard,
                4,
                2,
                150m);

        var deluxeRoomId =
            await AddRoomAsync(
                factory,
                scenario.HotelId,
                $"DLX-{shortId}",
                RoomType.Deluxe,
                4,
                2,
                200m);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        /*
         * BookingTestData creates:
         *
         * Standard
         * AdultsCapacity = 2
         * ChildrenCapacity = 1
         *
         * This query requires:
         *
         * Standard
         * Adults >= 3
         * Children >= 2
         *
         * Therefore:
         *
         * original room -> excluded by capacity
         * deluxe room   -> excluded by type
         * matching room -> returned
         */
        var query =
            new GetAvailableRoomsQuery(
                scenario.HotelId,
                RoomType.Standard,
                checkInDate,
                checkOutDate,
                3,
                2);

        // Act
        var result =
            await ExecuteQueryAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Single(result.Rooms);

        var room =
            result.Rooms.Single();

        Assert.Equal(
            matchingRoomId,
            room.Id);

        Assert.Equal(
            RoomType.Standard,
            room.RoomType);

        Assert.Equal(
            4,
            room.AdultsCapacity);

        Assert.Equal(
            2,
            room.ChildrenCapacity);

        Assert.Equal(
            150m,
            room.PricePerNight);

        Assert.DoesNotContain(
            result.Rooms,
            item =>
                item.Id == scenario.RoomId);

        Assert.DoesNotContain(
            result.Rooms,
            item =>
                item.Id == deluxeRoomId);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static async Task<GetAvailableRoomsResult>
        ExecuteQueryAsync(
            CustomWebApplicationFactory factory,
            GetAvailableRoomsQuery query)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    GetAvailableRoomsQueryHandler>();

        return await handler.HandleAsync(
            query,
            CancellationToken.None);
    }

    private static async Task AddPendingBookingAsync(
        CustomWebApplicationFactory factory,
        string userId,
        int hotelId,
        int roomId,
        decimal pricePerNight,
        DateOnly checkInDate,
        DateOnly checkOutDate)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var numberOfNights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var totalAmount =
            pricePerNight *
            numberOfNights;

        /*
         * This creates an active payment hold.
         *
         * AvailableRoomsQuery treats:
         *
         * PendingPayment && ExpiresAt > now
         *
         * as unavailable.
         */
        var booking =
            new Booking(
                userId,
                hotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Availability Test Customer",
                "availability@test.com",
                "+970599000000",
                null,
                totalAmount,
                now);

        booking.AddRoom(
            roomId,
            pricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<int> AddRoomAsync(
        CustomWebApplicationFactory factory,
        int hotelId,
        string roomNumber,
        RoomType roomType,
        int adultsCapacity,
        int childrenCapacity,
        decimal pricePerNight)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        var room =
            new Room(
                hotelId,
                roomNumber,
                roomType,
                "Integration test room",
                adultsCapacity,
                childrenCapacity,
                pricePerNight,
                DateTime.UtcNow);

        dbContext.Room.Add(
            room);

        await dbContext.SaveChangesAsync();

        return room.Id;
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
}