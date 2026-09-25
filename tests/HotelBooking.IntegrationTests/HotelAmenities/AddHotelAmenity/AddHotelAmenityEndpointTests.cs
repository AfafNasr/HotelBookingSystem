using HotelBooking.Api.Authentication;
using HotelBooking.Api.Hotels;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Amenities;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.HotelAmenities.AddHotelAmenity;

[Collection(DatabaseTestCollection.Name)]
public sealed class AddHotelAmenityEndpointTests
{
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task AddAmenity_WhenOwnerOwnsHotel_ShouldReturnNoContentAndPersistRelationship()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "amenity-owner-1",
            "amenity-owner-1@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        var amenityId = await CreateAmenityAsync(
            factory,
            "Swimming Pool");

        await AuthenticateAsync(
            client,
            "amenity-owner-1",
            TestPassword);

        var request = new AddHotelAmenityRequest(
            amenityId);

        var response = await client.PostAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/amenities",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var exists = await verificationDbContext.HotelAmenities
            .AsNoTracking()
            .AnyAsync(hotelAmenity =>
                hotelAmenity.HotelId == hotelId &&
                hotelAmenity.AmenityId == amenityId);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddAmenity_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/owner/hotels/1/amenities",
            new AddHotelAmenityRequest(1));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_WhenUserIsCustomer_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "amenity-customer",
            "amenity-customer@test.com",
            Roles.Customer);

        await AuthenticateAsync(
            client,
            "amenity-customer",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/owner/hotels/1/amenities",
            new AddHotelAmenityRequest(1));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_WhenHotelBelongsToAnotherOwner_ShouldReturnForbiddenAndNotPersistRelationship()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "requesting-owner",
            "requesting-owner@test.com",
            Roles.HotelOwner);

        var otherOwnerId = await CreateUserAsync(
            factory,
            "actual-owner",
            "actual-owner@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            otherOwnerId);

        var amenityId = await CreateAmenityAsync(
            factory,
            "Wi-Fi");

        await AuthenticateAsync(
            client,
            "requesting-owner",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/amenities",
            new AddHotelAmenityRequest(amenityId));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var exists = await verificationDbContext.HotelAmenities
            .AsNoTracking()
            .AnyAsync(hotelAmenity =>
                hotelAmenity.HotelId == hotelId &&
                hotelAmenity.AmenityId == amenityId);

        Assert.False(exists);
    }

    [Fact]
    public async Task AddAmenity_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "amenity-owner-hotel-not-found",
            "amenity-owner-hotel-not-found@test.com",
            Roles.HotelOwner);

        var amenityId = await CreateAmenityAsync(
            factory,
            "Gym");

        await AuthenticateAsync(
            client,
            "amenity-owner-hotel-not-found",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/owner/hotels/999999/amenities",
            new AddHotelAmenityRequest(amenityId));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_WhenAmenityDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "amenity-owner-amenity-not-found",
            "amenity-owner-amenity-not-found@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "amenity-owner-amenity-not-found",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/amenities",
            new AddHotelAmenityRequest(999999));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AddAmenity_WhenAmenityIsAlreadyAssigned_ShouldReturnConflictAndKeepSingleRelationship()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "amenity-owner-duplicate",
            "amenity-owner-duplicate@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        var amenityId = await CreateAmenityAsync(
            factory,
            "Parking");

        await AddHotelAmenityDirectlyAsync(
            factory,
            hotelId,
            amenityId);

        await AuthenticateAsync(
            client,
            "amenity-owner-duplicate",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/amenities",
            new AddHotelAmenityRequest(amenityId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var count = await verificationDbContext.HotelAmenities
            .AsNoTracking()
            .CountAsync(hotelAmenity =>
                hotelAmenity.HotelId == hotelId &&
                hotelAmenity.AmenityId == amenityId);

        Assert.Equal(1, count);
    }

    private static async Task<string> CreateUserAsync(
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

        var createResult = await userManager.CreateAsync(
            user,
            TestPassword);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                ", ",
                createResult.Errors.Select(
                    error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(
            user,
            role);

        Assert.True(roleResult.Succeeded);

        return user.Id;
    }

    private static async Task<int> CreateHotelAsync(
        CustomWebApplicationFactory factory,
        string ownerId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var city = new City(
            $"Amenity Test City {Guid.NewGuid()}",
            "PS",
            null,
            DateTime.UtcNow);

        dbContext.Cities.Add(city);

        await dbContext.SaveChangesAsync();

        var hotel = new Hotel(
            $"Amenity Test Hotel {Guid.NewGuid()}",
            city.Id,
            ownerId,
            4,
            HotelCategory.Luxury,
            DateTime.UtcNow);

        dbContext.Hotels.Add(hotel);

        await dbContext.SaveChangesAsync();

        return hotel.Id;
    }

    private static async Task<int> CreateAmenityAsync(
        CustomWebApplicationFactory factory,
        string name)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var amenity = new Amenity(
            $"{name} {Guid.NewGuid()}",
            DateTime.UtcNow);

        dbContext.Amenities.Add(amenity);

        await dbContext.SaveChangesAsync();

        return amenity.Id;
    }

    private static async Task AddHotelAmenityDirectlyAsync(
        CustomWebApplicationFactory factory,
        int hotelId,
        int amenityId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotelAmenity = new HotelAmenity(
            hotelId,
            amenityId);

        dbContext.HotelAmenities.Add(hotelAmenity);

        await dbContext.SaveChangesAsync();
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string username,
        string password)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                username,
                password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }
}
