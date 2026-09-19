using HotelBooking.Api.Authentication.Login;
using HotelBooking.Api.Cities.UpdateCity;
using HotelBooking.Domain.Cities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Cities.UpdateCity;

[Collection(DatabaseTestCollection.Name)]
public sealed class UpdateCityEndpointTests
{
    [Fact]
    public async Task UpdateCity_WhenAdminRequestIsValid_ShouldUpdateCity()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-city-admin";
        const string password = "Password123!";

        int cityId;
        DateTime originalCreatedAt;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = "update-city-admin@test.com"
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(roleResult.Succeeded);

            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var city = new City(
                "Old City Name",
                "JO",
                "Old Post Office",
                DateTime.UtcNow.AddDays(-1));

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            cityId = city.Id;
            originalCreatedAt = city.CreatedAt;
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

        var request = new UpdateCityRequest(
            "  Amman  ",
            " jo ",
            "  Amman Central Post Office  ");

        var response = await client.PutAsJsonAsync(
            $"/api/admin/cities/{cityId}",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var updatedCity = await verificationDbContext.Cities
            .AsNoTracking()
            .SingleAsync(city => city.Id == cityId);

        Assert.Equal("Amman", updatedCity.Name);
        Assert.Equal("JO", updatedCity.CountryCode);
        Assert.Equal(
            "Amman Central Post Office",
            updatedCity.PostOffice);

        Assert.Equal(
            originalCreatedAt,
            updatedCity.CreatedAt);

        Assert.NotNull(updatedCity.UpdatedAt);
        Assert.False(updatedCity.IsDeleted);
    }

    [Fact]
    public async Task UpdateCity_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new UpdateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PutAsJsonAsync(
            "/api/admin/cities/1",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateCity_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-city-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = username,
                Email = "update-city-customer@test.com"
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

        var request = new UpdateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PutAsJsonAsync(
            "/api/admin/cities/1",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateCity_WhenCityDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-city-admin";
        const string password = "Password123!";

        await CreateAdminAndAuthenticateAsync(
            factory,
            client,
            username,
            password,
            "update-city-admin@test.com");

        var request = new UpdateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PutAsJsonAsync(
            "/api/admin/cities/999999",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateCity_WhenCountryDoesNotExist_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-city-admin";
        const string password = "Password123!";

        int cityId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var city = new City(
                "Amman",
                "JO",
                null,
                DateTime.UtcNow);

            dbContext.Cities.Add(city);
            await dbContext.SaveChangesAsync();

            cityId = city.Id;
        }

        await CreateAdminAndAuthenticateAsync(
            factory,
            client,
            username,
            password,
            "update-city-admin@test.com");

        var request = new UpdateCityRequest(
            "Amman",
            "ZZ",
            null);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/cities/{cityId}",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateCity_WhenAnotherCityHasSameNameAndCountry_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "update-city-admin";
        const string password = "Password123!";

        int irbidId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var amman = new City(
                "Amman",
                "JO",
                null,
                DateTime.UtcNow);

            var irbid = new City(
                "Irbid",
                "JO",
                null,
                DateTime.UtcNow);

            dbContext.Cities.AddRange(amman, irbid);
            await dbContext.SaveChangesAsync();

            irbidId = irbid.Id;
        }

        await CreateAdminAndAuthenticateAsync(
            factory,
            client,
            username,
            password,
            "update-city-admin@test.com");

        var request = new UpdateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/cities/{irbidId}",
            request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    private static async Task CreateAdminAndAuthenticateAsync(
        CustomWebApplicationFactory factory,
        HttpClient client,
        string username,
        string password,
        string email)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = email
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

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
    }
}