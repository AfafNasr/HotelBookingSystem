using HotelBooking.Api.Authentication.Login;
using HotelBooking.Api.Hotels.CreateHotel;
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

namespace HotelBooking.IntegrationTests.Hotels.CreateHotel;

[Collection(DatabaseTestCollection.Name)]
public sealed class CreateHotelEndpointTests
{
    [Fact]
    public async Task CreateHotel_WhenAdminRequestIsValid_ShouldCreateHotel()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "hotel-admin";
        const string ownerUsername = "hotel-owner";
        const string password = "Password123!";

        int cityId;
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
                Email = "hotel-admin@test.com"
            };

            var adminCreateResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(adminCreateResult.Succeeded);

            var adminRoleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(adminRoleResult.Succeeded);

            var owner = new IdentityUser
            {
                UserName = ownerUsername,
                Email = "hotel-owner@test.com"
            };

            var ownerCreateResult =
                await userManager.CreateAsync(owner, password);

            Assert.True(ownerCreateResult.Succeeded);

            var customerRoleResult =
                await userManager.AddToRoleAsync(owner, "Customer");

            Assert.True(customerRoleResult.Succeeded);

            var ownerRoleResult =
                await userManager.AddToRoleAsync(owner, "HotelOwner");

            Assert.True(ownerRoleResult.Succeeded);

            ownerId = owner.Id;

            var city = new City(
                "Nablus",
                "PS",
                null,
                DateTime.UtcNow);

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            cityId = city.Id;
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(adminUsername, password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        var request = new CreateHotelRequest(
            "Royal Nablus Hotel",
            cityId,
            ownerId,
            5,
            HotelCategory.Luxury);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var createHotelResponse =
            await response.Content.ReadFromJsonAsync<CreateHotelResponse>();

        Assert.NotNull(createHotelResponse);
        Assert.True(createHotelResponse.HotelId > 0);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleOrDefaultAsync(hotel =>
                hotel.Id == createHotelResponse.HotelId);

        Assert.NotNull(hotel);

        Assert.Equal("Royal Nablus Hotel", hotel.Name);
        Assert.Equal(cityId, hotel.CityId);
        Assert.Equal(ownerId, hotel.OwnerId);
        Assert.Equal(5, hotel.StarRating);
        Assert.Equal(HotelCategory.Luxury, hotel.Category);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);

        Assert.False(hotel.IsDeleted);
    }

    [Fact]
    public async Task CreateHotel_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateHotelRequest(
            "Royal Nablus Hotel",
            1,
            "owner-id",
            5,
            HotelCategory.Luxury);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateHotel_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "hotel-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = username,
                Email = "hotel-customer@test.com"
            };

            var createResult =
                await userManager.CreateAsync(customer, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    "Customer");

            Assert.True(roleResult.Succeeded);
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        var request = new CreateHotelRequest(
            "Royal Nablus Hotel",
            1,
            "owner-id",
            5,
            HotelCategory.Luxury);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateHotel_WhenCityDoesNotExist_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "hotel-admin";
        const string ownerUsername = "hotel-owner";
        const string password = "Password123!";

        string ownerId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "hotel-admin@test.com"
            };

            var adminCreateResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(adminCreateResult.Succeeded);

            var adminRoleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(adminRoleResult.Succeeded);

            var owner = new IdentityUser
            {
                UserName = ownerUsername,
                Email = "hotel-owner@test.com"
            };

            var ownerCreateResult =
                await userManager.CreateAsync(owner, password);

            Assert.True(ownerCreateResult.Succeeded);

            var ownerRoleResult =
                await userManager.AddToRoleAsync(owner, "HotelOwner");

            Assert.True(ownerRoleResult.Succeeded);

            ownerId = owner.Id;
        }

        await AuthenticateAsync(
            client,
            adminUsername,
            password);

        var request = new CreateHotelRequest(
            "Royal Nablus Hotel",
            999999,
            ownerId,
            5,
            HotelCategory.Luxury);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateHotel_WhenSelectedUserIsNotHotelOwner_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "hotel-admin";
        const string customerUsername = "selected-customer";
        const string password = "Password123!";

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
                Email = "hotel-admin@test.com"
            };

            var adminCreateResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(adminCreateResult.Succeeded);

            var adminRoleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(adminRoleResult.Succeeded);

            var customer = new IdentityUser
            {
                UserName = customerUsername,
                Email = "selected-customer@test.com"
            };

            var customerCreateResult =
                await userManager.CreateAsync(customer, password);

            Assert.True(customerCreateResult.Succeeded);

            var customerRoleResult =
                await userManager.AddToRoleAsync(customer, "Customer");

            Assert.True(customerRoleResult.Succeeded);

            customerId = customer.Id;

            var city = new City(
                "Nablus",
                "PS",
                null,
                DateTime.UtcNow);

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            cityId = city.Id;
        }

        await AuthenticateAsync(
            client,
            adminUsername,
            password);

        var request = new CreateHotelRequest(
            "Royal Nablus Hotel",
            cityId,
            customerId,
            5,
            HotelCategory.Luxury);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
    factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        Assert.False(
            await verificationDbContext.Hotels
                .AsNoTracking()
                .AnyAsync());
    }

    [Fact]
    public async Task CreateHotel_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "hotel-admin";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = "hotel-admin@test.com"
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(roleResult.Succeeded);
        }

        await AuthenticateAsync(
            client,
            username,
            password);

        var request = new CreateHotelRequest(
            "",
            0,
            "",
            6,
            (HotelCategory)999);

        var response = await client.PostAsJsonAsync(
            "/api/admin/hotels",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string username,
        string password)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }
}