using HotelBooking.Api.Authentication.Login;
using HotelBooking.Api.Hotels.UpdateHotel;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Hotels.UpdateHotel;

[Collection(DatabaseTestCollection.Name)]
public sealed class UpdateHotelEndpointTests
{
    [Fact]
    public async Task UpdateHotel_WhenAdminRequestIsValid_ShouldUpdateHotel()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "update-hotel-admin";
        const string password = "Password123!";

        int hotelId;
        int newCityId;
        string newOwnerId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "update-hotel-admin@test.com"
            };

            var adminCreateResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(adminCreateResult.Succeeded);

            var adminRoleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(adminRoleResult.Succeeded);

            var originalOwner = new IdentityUser
            {
                UserName = "original-hotel-owner",
                Email = "original-hotel-owner@test.com"
            };

            var originalOwnerCreateResult =
                await userManager.CreateAsync(originalOwner, password);

            Assert.True(originalOwnerCreateResult.Succeeded);

            var originalOwnerRoleResult =
                await userManager.AddToRoleAsync(
                    originalOwner,
                    "HotelOwner");

            Assert.True(originalOwnerRoleResult.Succeeded);

            var newOwner = new IdentityUser
            {
                UserName = "new-hotel-owner",
                Email = "new-hotel-owner@test.com"
            };

            var newOwnerCreateResult =
                await userManager.CreateAsync(newOwner, password);

            Assert.True(newOwnerCreateResult.Succeeded);

            var newOwnerRoleResult =
                await userManager.AddToRoleAsync(
                    newOwner,
                    "HotelOwner");

            Assert.True(newOwnerRoleResult.Succeeded);

            newOwnerId = newOwner.Id;

            var originalCity = new City(
                "Nablus",
                "PS",
                null,
                DateTime.UtcNow);

            var newCity = new City(
                "Ramallah",
                "PS",
                null,
                DateTime.UtcNow);

            dbContext.Cities.AddRange(
                originalCity,
                newCity);

            await dbContext.SaveChangesAsync();

            newCityId = newCity.Id;

            var hotel = new Hotel(
                "Original Hotel",
                originalCity.Id,
                originalOwner.Id,
                5,
                HotelCategory.Luxury,
                DateTime.UtcNow);

            dbContext.Hotels.Add(hotel);

            await dbContext.SaveChangesAsync();

            hotelId = hotel.Id;
        }

        await AuthenticateAsync(
            client,
            adminUsername,
            password);

        var request = new UpdateHotelRequest(
            "Updated Hotel",
            newCityId,
            newOwnerId,
            4,
            HotelCategory.Boutique,
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/hotels/{hotelId}",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var updatedHotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Equal("Updated Hotel", updatedHotel.Name);
        Assert.Equal(newCityId, updatedHotel.CityId);
        Assert.Equal(newOwnerId, updatedHotel.OwnerId);
        Assert.Equal(4, updatedHotel.StarRating);
        Assert.Equal(
            HotelCategory.Boutique,
            updatedHotel.Category);

        Assert.Equal(32.2211m, updatedHotel.Latitude);
        Assert.Equal(35.2544m, updatedHotel.Longitude);

        Assert.NotNull(updatedHotel.UpdatedAt);
        Assert.False(updatedHotel.IsDeleted);
    }

    [Fact]
    public async Task UpdateHotel_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = CreateValidRequest();

        var response = await client.PutAsJsonAsync(
            "/api/admin/hotels/1",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-hotel-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = username,
                Email = "update-hotel-customer@test.com"
            };

            var createResult =
                await userManager.CreateAsync(
                    customer,
                    password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    "Customer");

            Assert.True(roleResult.Succeeded);
        }

        await AuthenticateAsync(
            client,
            username,
            password);

        var response = await client.PutAsJsonAsync(
            "/api/admin/hotels/1",
            CreateValidRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-missing-hotel-admin";
        const string password = "Password123!";

        await CreateAdminAsync(
            factory,
            username,
            "update-missing-hotel-admin@test.com",
            password);

        await AuthenticateAsync(
            client,
            username,
            password);

        var response = await client.PutAsJsonAsync(
            "/api/admin/hotels/999999",
            CreateValidRequest());

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WhenCityDoesNotExist_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "update-city-hotel-admin";
        const string password = "Password123!";

        int hotelId;
        string ownerId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "update-city-hotel-admin@test.com"
            };

            Assert.True(
                (await userManager.CreateAsync(admin, password))
                .Succeeded);

            Assert.True(
                (await userManager.AddToRoleAsync(admin, "Admin"))
                .Succeeded);

            var owner = new IdentityUser
            {
                UserName = "update-city-owner",
                Email = "update-city-owner@test.com"
            };

            Assert.True(
                (await userManager.CreateAsync(owner, password))
                .Succeeded);

            Assert.True(
                (await userManager.AddToRoleAsync(
                    owner,
                    "HotelOwner"))
                .Succeeded);

            ownerId = owner.Id;

            var city = new City(
                "Nablus",
                "PS",
                null,
                DateTime.UtcNow);

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            var hotel = new Hotel(
                "Hotel",
                city.Id,
                owner.Id,
                5,
                HotelCategory.Luxury,
                DateTime.UtcNow);

            dbContext.Hotels.Add(hotel);
            await dbContext.SaveChangesAsync();

            hotelId = hotel.Id;
        }

        await AuthenticateAsync(
            client,
            adminUsername,
            password);

        var request = new UpdateHotelRequest(
            "Updated Hotel",
            999999,
            ownerId,
            4,
            HotelCategory.Boutique,
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/hotels/{hotelId}",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WhenSelectedUserIsNotHotelOwner_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "update-owner-hotel-admin";
        const string password = "Password123!";

        int hotelId;
        int cityId;
        string customerId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "update-owner-hotel-admin@test.com"
            };

            Assert.True(
                (await userManager.CreateAsync(admin, password))
                .Succeeded);

            Assert.True(
                (await userManager.AddToRoleAsync(admin, "Admin"))
                .Succeeded);

            var originalOwner = new IdentityUser
            {
                UserName = "existing-hotel-owner",
                Email = "existing-hotel-owner@test.com"
            };

            Assert.True(
                (await userManager.CreateAsync(
                    originalOwner,
                    password))
                .Succeeded);

            Assert.True(
                (await userManager.AddToRoleAsync(
                    originalOwner,
                    "HotelOwner"))
                .Succeeded);

            var customer = new IdentityUser
            {
                UserName = "invalid-new-owner",
                Email = "invalid-new-owner@test.com"
            };

            Assert.True(
                (await userManager.CreateAsync(
                    customer,
                    password))
                .Succeeded);

            Assert.True(
                (await userManager.AddToRoleAsync(
                    customer,
                    "Customer"))
                .Succeeded);

            customerId = customer.Id;

            var city = new City(
                "Nablus",
                "PS",
                null,
                DateTime.UtcNow);

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            cityId = city.Id;

            var hotel = new Hotel(
                "Original Hotel",
                city.Id,
                originalOwner.Id,
                5,
                HotelCategory.Luxury,
                DateTime.UtcNow);

            dbContext.Hotels.Add(hotel);
            await dbContext.SaveChangesAsync();

            hotelId = hotel.Id;
        }

        await AuthenticateAsync(
            client,
            adminUsername,
            password);

        var request = new UpdateHotelRequest(
            "Updated Hotel",
            cityId,
            customerId,
            4,
            HotelCategory.Boutique,
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/hotels/{hotelId}",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static UpdateHotelRequest CreateValidRequest()
    {
        return new UpdateHotelRequest(
            "Updated Hotel",
            1,
            "hotel-owner-id",
            4,
            HotelCategory.Boutique,
            32.2211m,
            35.2544m);
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

    private static async Task CreateAdminAsync(
        CustomWebApplicationFactory factory,
        string username,
        string email,
        string password)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

        var admin = new IdentityUser
        {
            UserName = username,
            Email = email
        };

        var createResult =
            await userManager.CreateAsync(
                admin,
                password);

        Assert.True(createResult.Succeeded);

        var roleResult =
            await userManager.AddToRoleAsync(
                admin,
                "Admin");

        Assert.True(roleResult.Succeeded);
    }
}