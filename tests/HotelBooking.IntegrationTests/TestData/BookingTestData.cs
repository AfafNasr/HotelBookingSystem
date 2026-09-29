using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.TestData;

public static class BookingTestData
{
    public static async Task<BookingScenario> CreateAsync(
        IServiceProvider services,
        IdentityUser owner)
    {
        await using var scope =
            services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        var unique =
            Guid.NewGuid()
                .ToString("N")[..8];

        var country = await dbContext.Countries
            .SingleOrDefaultAsync(
                country => country.Code == "PS");

        if (country is null)
        {
            country = new Country(
                "PS",
                "Palestine");

            dbContext.Countries.Add(country);

            await dbContext.SaveChangesAsync();
        }

        var city = new City(
            $"Integration City {unique}",
            country.Code,
            null,
            now);

        dbContext.Cities.Add(city);

        await dbContext.SaveChangesAsync();

        var hotel = new Hotel(
            $"Integration Hotel {unique}",
            city.Id,
            owner.Id,
            5,
            HotelCategory.Luxury,
            now);

        dbContext.Hotels.Add(hotel);

        await dbContext.SaveChangesAsync();

        var room = new Room(
            hotel.Id,
            $"ROOM-{unique}",
            RoomType.Standard,
            "Integration test room",
            2,
            1,
            100m,
            now);

        dbContext.Room.Add(room);

        await dbContext.SaveChangesAsync();

        return new BookingScenario(
            hotel.Id,
            room.Id,
            room.PricePerNight);
    }
}

public sealed record BookingScenario(
    int HotelId,
    int RoomId,
    decimal PricePerNight);