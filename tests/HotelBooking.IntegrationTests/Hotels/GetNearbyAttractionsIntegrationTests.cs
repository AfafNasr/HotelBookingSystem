using System.Net;
using System.Text.Json;
using HotelBooking.Application.Hotels.GetNearbyAttractions;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelBooking.IntegrationTests.Hotels;

public sealed class GetNearbyAttractionsIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Existing hotel with coordinates returns nearby attractions
    // ============================================================

    [Fact]
    public async Task GetNearbyAttractions_WhenHotelHasCoordinates_ShouldReturnAttractions()
    {
        // Arrange
        var fakeAttractionsService =
            new FakeNearbyAttractionsService(
                [
                    new NearbyAttraction(
                        Name: "Clock Tower",
                        Address: "Nablus",
                        Latitude: 32.218845m,
                        Longitude: 35.261747m,
                        DistanceMeters: 736),

                    new NearbyAttraction(
                        Name: "Hammam as-Shifa",
                        Address: "Nablus Old City",
                        Latitude: 32.219273m,
                        Longitude: 35.259878m,
                        DistanceMeters: 555)
                ]);

        await using var baseFactory =
            new CustomWebApplicationFactory();

        using var factory =
            baseFactory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<
                                INearbyAttractionsService>();

                            services.AddSingleton<
                                INearbyAttractionsService>(
                                fakeAttractionsService);
                        });
                });

        var owner =
            await CreateUserAsync(factory);

        var hotelId =
            await CreateHotelAsync(
                factory,
                owner.Id,
                withCoordinates: true);

        var client =
            factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                $"/api/hotels/{hotelId}/nearby-attractions" +
                "?radiusMeters=3000&limit=10");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(json);

        var root =
            document.RootElement;

        Assert.Equal(
            JsonValueKind.Array,
            root.ValueKind);

        Assert.Equal(
            2,
            root.GetArrayLength());

        var first =
            root[0];

        Assert.Equal(
            "Clock Tower",
            first.GetProperty("name").GetString());

        Assert.Equal(
            "Nablus",
            first.GetProperty("address").GetString());

        Assert.Equal(
            32.218845m,
            first.GetProperty("latitude").GetDecimal());

        Assert.Equal(
            35.261747m,
            first.GetProperty("longitude").GetDecimal());

        Assert.Equal(
            736,
            first.GetProperty("distanceMeters").GetInt32());

        Assert.Equal(
            1,
            fakeAttractionsService.CallCount);

        Assert.Equal(
            31.5m,
            fakeAttractionsService.LastLatitude);

        Assert.Equal(
            35.1m,
            fakeAttractionsService.LastLongitude);

        Assert.Equal(
            3000,
            fakeAttractionsService.LastRadiusMeters);

        Assert.Equal(
            10,
            fakeAttractionsService.LastLimit);
    }

    // ============================================================
    // 2. Missing hotel returns 404
    // ============================================================

    [Fact]
    public async Task GetNearbyAttractions_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var fakeAttractionsService =
            new FakeNearbyAttractionsService(
                Array.Empty<NearbyAttraction>());

        await using var baseFactory =
            new CustomWebApplicationFactory();

        using var factory =
            baseFactory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<
                                INearbyAttractionsService>();

                            services.AddSingleton<
                                INearbyAttractionsService>(
                                fakeAttractionsService);
                        });
                });

        var client =
            factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                "/api/hotels/999999/nearby-attractions");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            0,
            fakeAttractionsService.CallCount);
    }

    // ============================================================
    // 3. Hotel without coordinates returns 409
    // ============================================================

    [Fact]
    public async Task GetNearbyAttractions_WhenHotelHasNoCoordinates_ShouldReturnConflict()
    {
        // Arrange
        var fakeAttractionsService =
            new FakeNearbyAttractionsService(
                Array.Empty<NearbyAttraction>());

        await using var baseFactory =
            new CustomWebApplicationFactory();

        using var factory =
            baseFactory.WithWebHostBuilder(
                builder =>
                {
                    builder.ConfigureServices(
                        services =>
                        {
                            services.RemoveAll<
                                INearbyAttractionsService>();

                            services.AddSingleton<
                                INearbyAttractionsService>(
                                fakeAttractionsService);
                        });
                });

        var owner =
            await CreateUserAsync(factory);

        var hotelId =
            await CreateHotelAsync(
                factory,
                owner.Id,
                withCoordinates: false);

        var client =
            factory.CreateClient();

        // Act
        var response =
            await client.GetAsync(
                $"/api/hotels/{hotelId}/nearby-attractions");

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.Equal(
            0,
            fakeAttractionsService.CallCount);
    }

    // ============================================================
    // Hotel setup
    // ============================================================

    private static async Task<int> CreateHotelAsync(
        WebApplicationFactory<Program> factory,
        string ownerId,
        bool withCoordinates)
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
                $"Nearby-City-{suffix}",
                country.Code,
                null,
                DateTime.UtcNow);

        dbContext.Cities.Add(
            city);

        await dbContext.SaveChangesAsync();

        var hotel =
            new Hotel(
                $"Nearby-Hotel-{suffix}",
                city.Id,
                ownerId,
                5,
                HotelCategory.Luxury,
                DateTime.UtcNow);

        if (withCoordinates)
        {
            hotel.CompleteProfile(
                "Nearby attractions integration hotel",
                "Integration hotel address",
                31.5m,
                35.1m,
                DateTime.UtcNow);
        }

        dbContext.Hotels.Add(
            hotel);

        await dbContext.SaveChangesAsync();

        return hotel.Id;
    }

    // ============================================================
    // Identity setup
    // ============================================================

    private static async Task<IdentityUser>
        CreateUserAsync(
            WebApplicationFactory<Program> factory)
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
                    $"nearby-{unique}",

                Email =
                    $"nearby-{unique}@test.com"
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

    // ============================================================
    // Fake external provider
    // ============================================================

    private sealed class FakeNearbyAttractionsService
        : INearbyAttractionsService
    {
        private readonly IReadOnlyCollection<
            NearbyAttraction> _attractions;

        public FakeNearbyAttractionsService(
            IReadOnlyCollection<
                NearbyAttraction> attractions)
        {
            _attractions = attractions;
        }

        public int CallCount
        {
            get;
            private set;
        }

        public decimal? LastLatitude
        {
            get;
            private set;
        }

        public decimal? LastLongitude
        {
            get;
            private set;
        }

        public int? LastRadiusMeters
        {
            get;
            private set;
        }

        public int? LastLimit
        {
            get;
            private set;
        }

        public Task<
            IReadOnlyCollection<NearbyAttraction>>
            GetAsync(
                decimal latitude,
                decimal longitude,
                int radiusMeters,
                int limit,
                CancellationToken cancellationToken)
        {
            CallCount++;

            LastLatitude =
                latitude;

            LastLongitude =
                longitude;

            LastRadiusMeters =
                radiusMeters;

            LastLimit =
                limit;

            return Task.FromResult(
                _attractions);
        }
    }
}