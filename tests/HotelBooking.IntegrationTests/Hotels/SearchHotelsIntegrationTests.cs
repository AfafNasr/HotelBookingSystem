using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Hotels;

public sealed class SearchHotelsIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Search by city + starting price
    // ============================================================

    [Fact]
    public async Task SearchHotels_WhenDestinationMatchesCity_ShouldReturnHotelsWithStartingPrice()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"SearchCity-{Guid.NewGuid():N}"[..19]);

        var hotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Search Hotel",
                starRating: 5);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "R-101",
            pricePerNight: 150m,
            adultsCapacity: 2,
            childrenCapacity: 1);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "R-102",
            pricePerNight: 90m,
            adultsCapacity: 2,
            childrenCapacity: 1);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(3),
                Adults: 2,
                Children: 0,
                Rooms: 1);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Single(result.Hotels);

        var item =
            result.Hotels.Single();

        Assert.Equal(
            hotel.Id,
            item.HotelId);

        Assert.Equal(
            "Search Hotel",
            item.Name);

        Assert.Equal(
            city.Name,
            item.CityName);

        Assert.Equal(
            5,
            item.StarRating);

        // Minimum available room price
        Assert.Equal(
            90m,
            item.StartingPricePerNight);
    }

    // ============================================================
    // 2. Fully unavailable hotel must not appear
    // ============================================================

    [Fact]
    public async Task SearchHotels_WhenOnlyRoomHasOverlappingActivePendingBooking_ShouldExcludeHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var user =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"BusyCity-{Guid.NewGuid():N}"[..17]);

        var hotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                user.Id,
                "Busy Hotel",
                starRating: 4);

        var room =
            await AddRoomAsync(
                factory,
                hotel.Id,
                "B-101",
                pricePerNight: 120m,
                adultsCapacity: 2,
                childrenCapacity: 1);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        await AddActivePendingBookingAsync(
            factory,
            user.Id,
            hotel.Id,
            room.Id,
            room.PricePerNight,
            checkInDate,
            checkOutDate);

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkOutDate,
                Adults: 2,
                Children: 0,
                Rooms: 1);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.DoesNotContain(
            result.Hotels,
            item =>
                item.HotelId == hotel.Id);
    }

    // ============================================================
    // 3. Star rating + price filters
    // ============================================================

    [Fact]
    public async Task SearchHotels_WhenFiltersAreApplied_ShouldReturnOnlyMatchingHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"FilterCity-{Guid.NewGuid():N}"[..19]);

        var expectedHotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Premium Hotel",
                starRating: 5);

        await AddRoomAsync(
            factory,
            expectedHotel.Id,
            "P-101",
            pricePerNight: 180m,
            adultsCapacity: 3,
            childrenCapacity: 1);

        var cheapHotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Cheap Hotel",
                starRating: 5);

        await AddRoomAsync(
            factory,
            cheapHotel.Id,
            "C-101",
            pricePerNight: 70m,
            adultsCapacity: 3,
            childrenCapacity: 1);

        var lowerStarHotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Four Star Hotel",
                starRating: 4);

        await AddRoomAsync(
            factory,
            lowerStarHotel.Id,
            "F-101",
            pricePerNight: 180m,
            adultsCapacity: 3,
            childrenCapacity: 1);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 2,
                Children: 0,
                Rooms: 1,
                MinPrice: 100m,
                MaxPrice: 200m,
                StarRating: 5);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Single(result.Hotels);

        Assert.Equal(
            expectedHotel.Id,
            result.Hotels.Single().HotelId);

        Assert.DoesNotContain(
            result.Hotels,
            item =>
                item.HotelId == cheapHotel.Id);

        Assert.DoesNotContain(
            result.Hotels,
            item =>
                item.HotelId == lowerStarHotel.Id);
    }

    // ============================================================
    // 4. Sorting + pagination + HasNextPage
    // ============================================================

    [Fact]
    public async Task SearchHotels_WhenSortedByPriceAndPaged_ShouldReturnCorrectPageAndHasNextPage()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"PageCity-{Guid.NewGuid():N}"[..17]);

        var cheap =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Cheap",
                starRating: 3);

        var medium =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Medium",
                starRating: 4);

        var expensive =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Expensive",
                starRating: 5);

        await AddRoomAsync(
            factory,
            cheap.Id,
            "CH-1",
            80m,
            2,
            0);

        await AddRoomAsync(
            factory,
            medium.Id,
            "MD-1",
            120m,
            2,
            0);

        await AddRoomAsync(
            factory,
            expensive.Id,
            "EX-1",
            200m,
            2,
            0);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var firstPageQuery =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 2,
                Children: 0,
                Rooms: 1,
                SortBy: HotelSearchSort.PriceLowToHigh,
                Page: 1,
                PageSize: 2);

        var firstPage =
            await ExecuteSearchAsync(
                factory,
                firstPageQuery);

        Assert.True(firstPage.Succeeded);

        Assert.Equal(
            2,
            firstPage.Hotels.Count);

        Assert.True(
            firstPage.HasNextPage);

        var firstPageHotels =
            firstPage.Hotels.ToArray();

        Assert.Equal(
            cheap.Id,
            firstPageHotels[0].HotelId);

        Assert.Equal(
            medium.Id,
            firstPageHotels[1].HotelId);

        var secondPageQuery =
            firstPageQuery with
            {
                Page = 2
            };

        var secondPage =
            await ExecuteSearchAsync(
                factory,
                secondPageQuery);

        Assert.True(secondPage.Succeeded);

        Assert.Single(
            secondPage.Hotels);

        Assert.False(
            secondPage.HasNextPage);

        Assert.Equal(
            expensive.Id,
            secondPage.Hotels.Single().HotelId);
    }

    [Fact]
    public async Task SearchHotels_WhenRequestedRoomCountCannotAccommodateGuests_ShouldExcludeHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"CapacityCity-{Guid.NewGuid():N}"[..19]);

        var hotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Capacity Hotel",
                starRating: 4);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "CAP-101",
            pricePerNight: 100m,
            adultsCapacity: 2,
            childrenCapacity: 0);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "CAP-102",
            pricePerNight: 100m,
            adultsCapacity: 2,
            childrenCapacity: 0);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 4,
                Children: 0,
                Rooms: 1);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.DoesNotContain(
            result.Hotels,
            item =>
                item.HotelId == hotel.Id);
    }

    [Fact]
    public async Task SearchHotels_WhenRequestedRoomsTogetherCanAccommodateGuests_ShouldReturnHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"CapacityOk-{Guid.NewGuid():N}"[..18]);

        var hotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Capacity Match Hotel",
                starRating: 4);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "OK-101",
            pricePerNight: 100m,
            adultsCapacity: 3,
            childrenCapacity: 1);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "OK-102",
            pricePerNight: 110m,
            adultsCapacity: 1,
            childrenCapacity: 2);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 4,
                Children: 3,
                Rooms: 2);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Contains(
            result.Hotels,
            item =>
                item.HotelId == hotel.Id);
    }

    [Fact]
    public async Task SearchHotels_WhenRequestedRoomsCannotAccommodateChildren_ShouldExcludeHotel()
    {
        await using var factory =
            new CustomWebApplicationFactory();

        var owner =
            await CreateUserAsync(factory);

        var city =
            await CreateCityAsync(
                factory,
                $"ChildCap-{Guid.NewGuid():N}"[..17]);

        var hotel =
            await CreateHotelAsync(
                factory,
                city.Id,
                owner.Id,
                "Children Capacity Hotel",
                starRating: 4);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "CHILD-101",
            pricePerNight: 100m,
            adultsCapacity: 4,
            childrenCapacity: 1);

        await AddRoomAsync(
            factory,
            hotel.Id,
            "CHILD-102",
            pricePerNight: 100m,
            adultsCapacity: 4,
            childrenCapacity: 1);

        var checkInDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(10));

        var query =
            new SearchHotelsQuery(
                city.Name,
                checkInDate,
                checkInDate.AddDays(2),
                Adults: 2,
                Children: 3,
                Rooms: 1);

        var result =
            await ExecuteSearchAsync(
                factory,
                query);

        Assert.True(result.Succeeded);

        Assert.DoesNotContain(
            result.Hotels,
            item =>
                item.HotelId == hotel.Id);
    }
    // ============================================================
    // Execute real application handler
    // ============================================================

    private static async Task<SearchHotelsResult>
        ExecuteSearchAsync(
            CustomWebApplicationFactory factory,
            SearchHotelsQuery query)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    SearchHotelsQueryHandler>();

        return await handler.HandleAsync(
            query,
            CancellationToken.None);
    }

    // ============================================================
    // Database setup
    // ============================================================

    private static async Task<City>
        CreateCityAsync(
            CustomWebApplicationFactory factory,
            string name)
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

        var city =
            new City(
                name,
                country.Code,
                null,
                DateTime.UtcNow);

        dbContext.Cities.Add(
            city);

        await dbContext.SaveChangesAsync();

        return city;
    }

    private static async Task<Hotel>
        CreateHotelAsync(
            CustomWebApplicationFactory factory,
            int cityId,
            string ownerId,
            string name,
            int starRating)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel =
            new Hotel(
                name,
                cityId,
                ownerId,
                starRating,
                HotelCategory.Luxury,
                DateTime.UtcNow);

        dbContext.Hotels.Add(
            hotel);

        await dbContext.SaveChangesAsync();

        return hotel;
    }

    private static async Task<Room>
        AddRoomAsync(
            CustomWebApplicationFactory factory,
            int hotelId,
            string roomNumber,
            decimal pricePerNight,
            int adultsCapacity,
            int childrenCapacity)
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
                RoomType.Standard,
                "Integration search room",
                adultsCapacity,
                childrenCapacity,
                pricePerNight,
                DateTime.UtcNow);

        dbContext.Room.Add(
            room);

        await dbContext.SaveChangesAsync();

        return room;
    }

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
                "Search Test Guest",
                "search.guest@test.com",
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
            Guid.NewGuid().ToString("N");

        var user =
            new IdentityUser
            {
                UserName =
                    $"search-{unique}",

                Email =
                    $"search-{unique}@test.com"
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
}