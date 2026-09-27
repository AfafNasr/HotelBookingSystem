using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Reviews;
using HotelBooking.Domain.Rooms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Persistence.Seed;

public sealed class PerformanceDataSeeder
{
    private const int RandomSeed = 20260927;

    private const int CityCount = 10;
    private const int OwnerCount = 20;
    private const int CustomerCount = 100;
    private const int HotelCount = 200;
    private const int RoomsPerHotel = 25;
    private const int BookingCount = 20_000;
    private const int DealCount = 60;

    private static readonly DateOnly BenchmarkDate =
        new(2026, 10, 10);

    private const string PerformanceCountryCode = "PX";
    private const string UserPassword = "Performance123!";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<PerformanceDataSeeder> _logger;

    public PerformanceDataSeeder(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        ILogger<PerformanceDataSeeder> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var performanceDataAlreadyExists =
            await _dbContext.Hotels
                .AsNoTracking()
                .AnyAsync(
                    hotel =>
                        hotel.Name.StartsWith("Perf Hotel "),
                    cancellationToken);

        if (performanceDataAlreadyExists)
        {
            _logger.LogInformation(
                "Performance data already exists. Seeding was skipped.");

            return;
        }

        var random =
            new Random(RandomSeed);

        var now =
            DateTime.UtcNow;

        _logger.LogInformation(
            "Starting performance data seeding.");

        var ownerIds =
            await CreateUsersAsync(
                prefix: "perf-owner",
                count: OwnerCount,
                role: Roles.HotelOwner);

        var customerIds =
            await CreateUsersAsync(
                prefix: "perf-customer",
                count: CustomerCount,
                role: Roles.Customer);

        var cities =
            await CreateCitiesAsync(
                now,
                cancellationToken);

        var hotels =
            await CreateHotelsAsync(
                cities,
                ownerIds,
                random,
                now,
                cancellationToken);

        var roomsByHotel =
            await CreateRoomsAsync(
                hotels,
                random,
                now,
                cancellationToken);

        await CreateDealsAsync(
            hotels,
            random,
            now,
            cancellationToken);

        await CreateBookingsAsync(
            roomsByHotel,
            customerIds,
            random,
            now,
            cancellationToken);

        _logger.LogInformation(
            "Performance data seeding completed. " +
            "{CityCount} cities, {HotelCount} hotels, " +
            "{RoomCount} rooms and {BookingCount} bookings were created.",
            CityCount,
            HotelCount,
            HotelCount * RoomsPerHotel,
            BookingCount);
    }

    private async Task<IReadOnlyList<string>> CreateUsersAsync(
        string prefix,
        int count,
        string role)
    {
        var userIds =
            new List<string>(count);

        for (var index = 1; index <= count; index++)
        {
            var username =
                $"{prefix}-{index:000}";

            var email =
                $"{username}@performance.local";

            var user =
                await _userManager.FindByNameAsync(
                    username);

            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = username,
                    Email = email
                };

                var createResult =
                    await _userManager.CreateAsync(
                        user,
                        UserPassword);

                EnsureIdentitySucceeded(
                    createResult,
                    $"Failed to create performance user '{username}'.");
            }

            if (!await _userManager.IsInRoleAsync(
                    user,
                    role))
            {
                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        role);

                EnsureIdentitySucceeded(
                    roleResult,
                    $"Failed to assign role '{role}' to '{username}'.");
            }

            userIds.Add(user.Id);
        }

        _logger.LogInformation(
            "Created or reused {UserCount} performance users for role {Role}.",
            count,
            role);

        return userIds;
    }

    private async Task<IReadOnlyList<City>> CreateCitiesAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var country =
            await _dbContext.Countries
                .SingleOrDefaultAsync(
                    country =>
                        country.Code ==
                        PerformanceCountryCode,
                    cancellationToken);

        if (country is null)
        {
            country =
                new Country(
                    PerformanceCountryCode,
                    "Performance Country");

            _dbContext.Countries.Add(country);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        var cities =
            new List<City>(CityCount);

        for (var index = 1;
             index <= CityCount;
             index++)
        {
            cities.Add(
                new City(
                    $"Performance City {index:00}",
                    PerformanceCountryCode,
                    $"P{index:000}",
                    now));
        }

        _dbContext.Cities.AddRange(cities);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Created {CityCount} performance cities.",
            cities.Count);

        return cities;
    }

    private async Task<IReadOnlyList<Hotel>> CreateHotelsAsync(
        IReadOnlyList<City> cities,
        IReadOnlyList<string> ownerIds,
        Random random,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var categories =
            Enum.GetValues<HotelCategory>();

        var hotels =
            new List<Hotel>(HotelCount);

        for (var index = 1;
             index <= HotelCount;
             index++)
        {
            var city =
                cities[(index - 1) % cities.Count];

            var ownerId =
                ownerIds[(index - 1) % ownerIds.Count];

            var hotel =
                new Hotel(
                    $"Perf Hotel {index:000}",
                    city.Id,
                    ownerId,
                    random.Next(1, 6),
                    categories[
                        random.Next(categories.Length)],
                    now);

            hotel.CompleteProfile(
                $"Performance hotel {index} used for load testing.",
                $"{index} Performance Street",
                latitude: null,
                longitude: null,
                updatedAt: now);

            hotels.Add(hotel);
        }

        _dbContext.Hotels.AddRange(hotels);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Created {HotelCount} performance hotels.",
            hotels.Count);

        return hotels;
    }

    private async Task<
        IReadOnlyDictionary<int, IReadOnlyList<RoomSeedItem>>>
        CreateRoomsAsync(
            IReadOnlyList<Hotel> hotels,
            Random random,
            DateTime now,
            CancellationToken cancellationToken)
    {
        var roomTypes =
            Enum.GetValues<RoomType>();

        var rooms =
            new List<Room>(
                hotels.Count * RoomsPerHotel);

        foreach (var hotel in hotels)
        {
            for (var roomIndex = 1;
                 roomIndex <= RoomsPerHotel;
                 roomIndex++)
            {
                var roomType =
                    roomTypes[
                        random.Next(roomTypes.Length)];

                var adultsCapacity =
                    random.Next(1, 5);

                var childrenCapacity =
                    random.Next(0, 4);

                var price =
                    random.Next(60, 501);

                rooms.Add(
                    new Room(
                        hotel.Id,
                        roomIndex.ToString("000"),
                        roomType,
                        $"Performance room {roomIndex}.",
                        adultsCapacity,
                        childrenCapacity,
                        price,
                        now));
            }
        }

        _dbContext.Room.AddRange(rooms);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var roomsByHotel =
            rooms
                .GroupBy(room => room.HotelId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        (IReadOnlyList<RoomSeedItem>)group
                            .Select(room =>
                                new RoomSeedItem(
                                    room.Id,
                                    room.HotelId,
                                    room.PricePerNight))
                            .ToArray());

        _dbContext.ChangeTracker.Clear();

        _logger.LogInformation(
            "Created {RoomCount} performance rooms.",
            rooms.Count);

        return roomsByHotel;
    }

    private async Task CreateDealsAsync(
        IReadOnlyList<Hotel> hotels,
        Random random,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var deals =
            new List<Deal>(DealCount);

        for (var index = 0;
             index < DealCount;
             index++)
        {
            var hotel =
                hotels[index % hotels.Count];

            var discountPercentage =
                random.Next(5, 31);

            deals.Add(
                new Deal(
                    hotel.Id,
                    discountPercentage,
                    BenchmarkDate.AddDays(-30),
                    BenchmarkDate.AddDays(30),
                    now));
        }

        _dbContext.Deals.AddRange(deals);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _dbContext.ChangeTracker.Clear();

        _logger.LogInformation(
            "Created {DealCount} performance deals.",
            deals.Count);
    }

    private async Task CreateBookingsAsync(
        IReadOnlyDictionary<
            int,
            IReadOnlyList<RoomSeedItem>> roomsByHotel,
        IReadOnlyList<string> customerIds,
        Random random,
        DateTime now,
        CancellationToken cancellationToken)
    {
        const int batchSize = 500;

        var hotelIds =
            roomsByHotel.Keys.ToArray();

        for (var batchStart = 0;
             batchStart < BookingCount;
             batchStart += batchSize)
        {
            var batchEnd =
                Math.Min(
                    batchStart + batchSize,
                    BookingCount);

            var bookings =
                new List<SeededBooking>(
                    batchEnd - batchStart);

            for (var index = batchStart;
                 index < batchEnd;
                 index++)
            {
                var hotelId =
                    hotelIds[
                        random.Next(hotelIds.Length)];

                var hotelRooms =
                    roomsByHotel[hotelId];

                var selectedRooms =
                    SelectDistinctRooms(
                        hotelRooms,
                        random,
                        random.Next(1, 4));

                var checkInDate =
                    CreateCheckInDate(
                        index,
                        random);

                var numberOfNights =
                    random.Next(1, 8);

                var checkOutDate =
                    checkInDate.AddDays(
                        numberOfNights);

                var customerId =
                    customerIds[
                        random.Next(customerIds.Count)];
                var statusBucket =
    index % 100;

                var isPendingPayment =
                    statusBucket >= 65 &&
                    statusBucket < 80;

                var createdAt =
                    isPendingPayment
                        ? now.AddMinutes(-5)
                        : now.AddDays(
                                -random.Next(0, 120))
                             .AddMinutes(
                                -random.Next(0, 1440));

                var expiresAt =
                    isPendingPayment
                        ? now.AddMinutes(10)
                        : createdAt.AddMinutes(15);

                var totalAmount =
                    selectedRooms.Sum(
                        room =>
                            room.PricePerNight *
                            numberOfNights);

                var booking =
                    new Booking(
                        customerId,
                        hotelId,
                        checkInDate,
                        checkOutDate,
                        expiresAt,
                        $"Performance Guest {index + 1}",
                        $"guest-{index + 1}@performance.local",
                        $"+1000{index + 1:000000}",
                        null,
                        totalAmount,
                        createdAt);

                foreach (var room in selectedRooms)
                {
                    booking.AddRoom(
                        room.Id,
                        room.PricePerNight);
                }

                var status =
                    ApplyBookingStatus(
                        booking,
                        index,
                        createdAt,
                        expiresAt);

                _dbContext.Bookings.Add(
                    booking);

                bookings.Add(
                    new SeededBooking(
                        booking,
                        status,
                        checkOutDate));
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            var reviews =
                new List<Review>();

            foreach (var seededBooking in bookings)
            {
                if (seededBooking.Status !=
                    BookingStatus.Confirmed)
                {
                    continue;
                }

                if (seededBooking.CheckOutDate >=
                    BenchmarkDate)
                {
                    continue;
                }

                if (random.NextDouble() > 0.35)
                {
                    continue;
                }

                reviews.Add(
                    new Review(
                        seededBooking.Booking.Id,
                        random.Next(1, 6),
                        "Performance test review.",
                        now.AddDays(
                            -random.Next(0, 60))));
            }

            if (reviews.Count > 0)
            {
                _dbContext.Reviews.AddRange(
                    reviews);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            _dbContext.ChangeTracker.Clear();

            _logger.LogInformation(
                "Seeded {SeededBookings}/{TotalBookings} performance bookings.",
                batchEnd,
                BookingCount);
        }
    }

    private static DateOnly CreateCheckInDate(
        int bookingIndex,
        Random random)
    {
        // Every fifth booking is deliberately clustered
        // around the benchmark search window.
        if (bookingIndex % 5 == 0)
        {
            return BenchmarkDate.AddDays(
                random.Next(-3, 4));
        }

        return BenchmarkDate.AddDays(
            random.Next(-120, 61));
    }

    private static BookingStatus ApplyBookingStatus(
        Booking booking,
        int bookingIndex,
        DateTime createdAt,
        DateTime expiresAt)
    {
        var bucket =
            bookingIndex % 100;

        if (bucket < 65)
        {
            booking.Confirm(
                $"PERF-{bookingIndex + 1:000000}",
                createdAt.AddMinutes(5));

            return BookingStatus.Confirmed;
        }

        if (bucket < 80)
        {
            // Keep a smaller percentage of active holds.
            // They must have an expiry in the future to
            // affect availability queries.
            return BookingStatus.PendingPayment;
        }

        if (bucket < 90)
        {
            booking.Cancel(
                createdAt.AddMinutes(5));

            return BookingStatus.Cancelled;
        }

        booking.Expire(
            expiresAt.AddMinutes(1));

        return BookingStatus.Expired;
    }

    private static IReadOnlyList<RoomSeedItem>
        SelectDistinctRooms(
            IReadOnlyList<RoomSeedItem> rooms,
            Random random,
            int count)
    {
        count =
            Math.Min(
                count,
                rooms.Count);

        var selected =
            new List<RoomSeedItem>(count);

        var selectedIndexes =
            new HashSet<int>();

        while (selected.Count < count)
        {
            var index =
                random.Next(rooms.Count);

            if (!selectedIndexes.Add(index))
            {
                continue;
            }

            selected.Add(
                rooms[index]);
        }

        return selected;
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors =
            string.Join(
                ", ",
                result.Errors.Select(
                    error => error.Description));

        throw new InvalidOperationException(
            $"{message} {errors}");
    }

    private sealed record RoomSeedItem(
        int Id,
        int HotelId,
        decimal PricePerNight);

    private sealed record SeededBooking(
        Booking Booking,
        BookingStatus Status,
        DateOnly CheckOutDate);
}