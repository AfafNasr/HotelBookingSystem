using HotelBooking.Api.Authentication;
using HotelBooking.Api.Cities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Cities.CreateCity;

[Collection(DatabaseTestCollection.Name)]
public sealed class CreateCityEndpointTests
{
    [Fact]
    public async Task CreateCity_WhenAdminRequestIsValid_ShouldCreateCity()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "city-admin";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = "city-admin@test.com"
            };

            var createResult =
                await userManager.CreateAsync(admin, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            Assert.True(roleResult.Succeeded);
        }

        var loginRequest = new LoginRequest(
            username,
            password);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

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

        var request = new CreateCityRequest(
            "Amman",
            "JO",
            "Amman Central Post Office");

        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var createCityResponse =
            await response.Content.ReadFromJsonAsync<CreateCityResponse>();

        Assert.NotNull(createCityResponse);
        Assert.True(createCityResponse.Id > 0);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var city = await dbContext.Cities
            .AsNoTracking()
            .SingleOrDefaultAsync(city =>
                city.Id == createCityResponse.Id);

        Assert.NotNull(city);
        Assert.Equal("Amman", city.Name);
        Assert.Equal("JO", city.CountryCode);
        Assert.Equal(
            "Amman Central Post Office",
            city.PostOffice);
        Assert.False(city.IsDeleted);
    }

    [Fact]
    public async Task CreateCity_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCity_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "city-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = username,
                Email = "city-customer@test.com"
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

        var loginRequest = new LoginRequest(
            username,
            password);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            loginRequest);

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

        var request = new CreateCityRequest(
            "Amman",
            "JO",
            null);

        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCity_WhenCountryDoesNotExist_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "city-admin";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = "city-admin@test.com"
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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        var request = new CreateCityRequest(
            "Test City",
            "ZZ",
            null);

        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCity_WhenCityAlreadyExists_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "city-admin";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = "city-admin@test.com"
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

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);

        var firstRequest = new CreateCityRequest(
            "Amman",
            "JO",
            null);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/admin/cities",
            firstRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var duplicateRequest = new CreateCityRequest(
            "Amman",
            "JO",
            "Different Post Office");

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/admin/cities",
            duplicateRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
    }
}
