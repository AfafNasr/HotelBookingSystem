using System.Security.Claims;
using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Hotels;

public sealed class GetHotelDetailsIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Returns hotel details, reviews and available room counts
    // ============================================================

    [Fact]
    public async Task GetHotelDetails_WhenHotelExists_ShouldReturnCompleteDetails()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var setup =
            await CreateHotelAsync(
                factory,
                owner.Id);

        var standardRoom1 =
            await AddRoomAsync(
                factory,
                setup.HotelId,
                "STD-101",
                RoomType.Standard,
                2,
                1,
                100m);

        var standardRoom2 =
            await AddRoomAsync(
                factory,
                setup.HotelId,
                "STD-102",
                RoomType.Standard,
                2,
                1,
                120m);

        var deluxeRoom =
            await AddRoomAsync(
                factory,
                setup.HotelId,
                "DLX-101",
                RoomType.Deluxe,
                3,
                1,
                180m);

        var customer =
            await CreateUserAsync(factory);

        await AddReviewAsync(
            factory,
            customer.Id,
            setup.HotelId,
            standardRoom1,
            rating: 5,
            comment: "Excellent");

        await AddReviewAsync(
            factory,
            customer.Id,
            setup.HotelId,
            standardRoom2,
            rating: 3,
            comment: "Good");

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new GetHotelDetailsQuery(
                setup.HotelId,
                checkInDate,
                checkInDate.AddDays(3),
                Adults: 2,
                Children: 0,
                Rooms: 1);

        // Act
        var result =
            await ExecuteHandlerAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Hotel);

        var hotel =
            result.Hotel!;

        Assert.Equal(
            setup.HotelId,
            hotel.Id);

        Assert.Equal(
            setup.HotelName,
            hotel.Name);

        Assert.Equal(
            setup.CityName,
            hotel.CityName);

        Assert.Equal(
            5,
            hotel.StarRating);

        Assert.Equal(
            HotelCategory.Luxury,
            hotel.Category);

        Assert.Equal(
            "Integration hotel description",
            hotel.Description);

        Assert.Equal(
            "Integration hotel address",
            hotel.Address);

        Assert.Equal(
            31.5m,
            hotel.Latitude);

        Assert.Equal(
            35.1m,
            hotel.Longitude);

        Assert.Equal(
            2,
            hotel.ReviewCount);

        Assert.Equal(
            4m,
            hotel.AverageGuestRating);

        Assert.Equal(
            2,
            hotel.RecentReviews.Count);

        var standardSummary =
            Assert.Single(
                hotel.AvailableRooms,
                room =>
                    room.RoomType ==
                    RoomType.Standard);

        Assert.Equal(
            2,
            standardSummary.AvailableCount);

        var deluxeSummary =
            Assert.Single(
                hotel.AvailableRooms,
                room =>
                    room.RoomType ==
                    RoomType.Deluxe);

        Assert.Equal(
            1,
            deluxeSummary.AvailableCount);

        Assert.Contains(
            hotel.RecentReviews,
            review =>
                review.Rating == 5 &&
                review.Comment == "Excellent");

        Assert.Contains(
            hotel.RecentReviews,
            review =>
                review.Rating == 3 &&
                review.Comment == "Good");
    }

    // ============================================================
    // 2. Active overlapping booking reduces available room count
    // ============================================================

    [Fact]
    public async Task GetHotelDetails_WhenRoomHasOverlappingActiveBooking_ShouldReduceAvailableRoomCount()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var customer =
            await CreateUserAsync(factory);

        var setup =
            await CreateHotelAsync(
                factory,
                owner.Id);

        var firstRoomId =
            await AddRoomAsync(
                factory,
                setup.HotelId,
                "STD-201",
                RoomType.Standard,
                2,
                1,
                100m);

        await AddRoomAsync(
            factory,
            setup.HotelId,
            "STD-202",
            RoomType.Standard,
            2,
            1,
            120m);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        await AddActivePendingBookingAsync(
            factory,
            customer.Id,
            setup.HotelId,
            firstRoomId,
            100m,
            checkInDate,
            checkOutDate);

        var query =
            new GetHotelDetailsQuery(
                setup.HotelId,
                checkInDate,
                checkOutDate,
                Adults: 2,
                Children: 0,
                Rooms: 1);

        // Act
        var result =
            await ExecuteHandlerAsync(
                factory,
                query);

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Hotel);

        var standardSummary =
            Assert.Single(
                result.Hotel!.AvailableRooms,
                room =>
                    room.RoomType ==
                    RoomType.Standard);

        Assert.Equal(
            1,
            standardSummary.AvailableCount);
    }

    // ============================================================
    // 3. Missing hotel
    // ============================================================

    [Fact]
    public async Task GetHotelDetails_WhenHotelDoesNotExist_ShouldReturnNotFoundResult()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new GetHotelDetailsQuery(
                999999,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 2,
                Children: 0,
                Rooms: 1);

        // Act
        var result =
            await ExecuteHandlerAsync(
                factory,
                query);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Hotel);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);
    }

    // ============================================================
    // 4. Authenticated user visit is persisted
    // ============================================================

    [Fact]
    public async Task GetHotelDetails_WhenUserIsAuthenticated_ShouldRecordRecentlyVisitedHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var customer =
            await CreateUserAsync(factory);

        var setup =
            await CreateHotelAsync(
                factory,
                owner.Id);

        await AddRoomAsync(
            factory,
            setup.HotelId,
            "STD-301",
            RoomType.Standard,
            2,
            0,
            100m);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new GetHotelDetailsQuery(
                setup.HotelId,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 2,
                Children: 0,
                Rooms: 1);

        // Act
        var result =
            await ExecuteHandlerAsUserAsync(
                factory,
                query,
                customer.Id);

        // Assert
        Assert.True(result.Succeeded);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var visit =
            await dbContext.RecentlyVisitedHotels
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.UserId == customer.Id &&
                        item.HotelId == setup.HotelId);

        Assert.NotNull(visit);

        Assert.Equal(
            customer.Id,
            visit.UserId);

        Assert.Equal(
            setup.HotelId,
            visit.HotelId);

        Assert.True(
            visit.LastVisitedAt > DateTime.MinValue);
    }

    // ============================================================
    // Handler execution
    // ============================================================

    private static async Task<GetHotelDetailsResult>
        ExecuteHandlerAsync(
            CustomWebApplicationFactory factory,
            GetHotelDetailsQuery query)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    GetHotelDetailsQueryHandler>();

        return await handler.HandleAsync(
            query,
            CancellationToken.None);
    }

    private static async Task<GetHotelDetailsResult>
        ExecuteHandlerAsUserAsync(
            CustomWebApplicationFactory factory,
            GetHotelDetailsQuery query,
            string userId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var httpContextAccessor =
            scope.ServiceProvider
                .GetRequiredService<
                    IHttpContextAccessor>();

        var identity =
            new ClaimsIdentity(
                [
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId)
                ],
                authenticationType: "IntegrationTest");

        httpContextAccessor.HttpContext =
            new DefaultHttpContext
            {
                User =
                    new ClaimsPrincipal(identity)
            };

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    GetHotelDetailsQueryHandler>();

        return await handler.HandleAsync(
            query,
            CancellationToken.None);
    }

    // ============================================================
    // Hotel setup
    // ============================================================

    private static async Task<HotelSetup>
        CreateHotelAsync(
            CustomWebApplicationFactory factory,
            string ownerId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var country =
            await dbContext.Countries
                .SingleOrDefaultAsync(
                    country =>
                        country.Code == "PS");

        if (country is null)
        {
            country =
                new Country(
                    "PS",
                    "Palestine");

            dbContext.Countries.Add(
                country);

            await dbContext.SaveChangesAsync();
        }

        var suffix =
            Guid.NewGuid()
                .ToString("N")[..6];

        var city =
            new City(
                $"City-{suffix}",
                country.Code,
                null,
                DateTime.UtcNow);

        dbContext.Cities.Add(
            city);

        await dbContext.SaveChangesAsync();

        var hotel =
            new Hotel(
                $"Hotel-{suffix}",
                city.Id,
                ownerId,
                5,
                HotelCategory.Luxury,
                DateTime.UtcNow);

        hotel.CompleteProfile(
            "Integration hotel description",
            "Integration hotel address",
            31.5m,
            35.1m,
            DateTime.UtcNow);

        dbContext.Hotels.Add(
            hotel);

        await dbContext.SaveChangesAsync();

        return new HotelSetup(
            hotel.Id,
            hotel.Name,
            city.Name);
    }

    private static async Task<int>
        AddRoomAsync(
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
                .GetRequiredService<ApplicationDbContext>();

        var room =
            new Room(
                hotelId,
                roomNumber,
                roomType,
                "Integration room",
                adultsCapacity,
                childrenCapacity,
                pricePerNight,
                DateTime.UtcNow);

        dbContext.Room.Add(room);

        await dbContext.SaveChangesAsync();

        return room.Id;
    }

    // ============================================================
    // Booking / Review setup
    // ============================================================

    private static async Task
        AddActivePendingBookingAsync(
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
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var nights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var booking =
            new Booking(
                userId,
                hotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Hotel Details Guest",
                "details.guest@test.com",
                "+970599000000",
                null,
                pricePerNight * nights,
                now);

        booking.AddRoom(
            roomId,
            pricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();
    }

    private static async Task
        AddReviewAsync(
            CustomWebApplicationFactory factory,
            string userId,
            int hotelId,
            int roomId,
            int rating,
            string comment)
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
                now.AddDays(-10));

        var checkOutDate =
            checkInDate.AddDays(2);

        var booking =
            new Booking(
                userId,
                hotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Review Guest",
                "review@test.com",
                "+970599000000",
                null,
                200m,
                now.AddDays(-15));

        booking.AddRoom(
            roomId,
            100m);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        var review =
            new Review(
                booking.Id,
                rating,
                comment,
                DateTime.UtcNow);

        dbContext.Reviews.Add(
            review);

        await dbContext.SaveChangesAsync();
    }

    // ============================================================
    // Identity setup
    // ============================================================

    private static async Task<IdentityUser>
        CreateUserAsync(
            CustomWebApplicationFactory factory)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<IdentityUser>>();

        var unique =
            Guid.NewGuid()
                .ToString("N");

        var user =
            new IdentityUser
            {
                UserName =
                    $"details-{unique}",

                Email =
                    $"details-{unique}@test.com"
            };

        var result =
            await userManager.CreateAsync(
                user,
                Password);

        Assert.True(
            result.Succeeded,
            string.Join(
                ", ",
                result.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    private sealed record HotelSetup(
        int HotelId,
        string HotelName,
        string CityName);
}