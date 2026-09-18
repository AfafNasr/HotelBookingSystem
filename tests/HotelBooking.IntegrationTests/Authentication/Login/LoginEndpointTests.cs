using HotelBooking.Api.Authentication.Login;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace HotelBooking.IntegrationTests.Authentication.Login;

[Collection(DatabaseTestCollection.Name)]
public sealed class LoginEndpointTests
{
    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturnAccessToken()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "login-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var user = new IdentityUser
            {
                UserName = username,
                Email = "login-customer@test"
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(user, "Customer");

            Assert.True(roleResult.Succeeded);
        }

        var request = new LoginRequest(
            username,
            password);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
        Assert.True(loginResponse.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WhenPasswordIsIncorrect_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "login-customer";
        const string correctPassword = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var user = new IdentityUser
            {
                UserName = username,
                Email = "login-customer@test"
            };

            var createResult =
                await userManager.CreateAsync(user, correctPassword);

            Assert.True(createResult.Succeeded);
        }

        var request = new LoginRequest(
            username,
            "WrongPassword123!");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUsernameDoesNotExist_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new LoginRequest(
            "unknown-user",
            "Password123!");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUsernameIsEmpty_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new LoginRequest(
            "",
            "Password123!");

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturnTokenWithExpectedClaims()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "claims-customer";
        const string password = "Password123!";

        string userId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var user = new IdentityUser
            {
                UserName = username,
                Email = "claims-customer@test"
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(user, "Customer");

            Assert.True(roleResult.Succeeded);

            userId = user.Id;
        }

        var request = new LoginRequest(username, password);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResponse);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(loginResponse.AccessToken);

        Assert.Equal(userId, token.Subject);

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Name &&
                claim.Value == username);

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Role &&
                claim.Value == "Customer");

        Assert.Equal("HotelBooking.Api.Tests", token.Issuer);
        Assert.Contains("HotelBooking.Api.Tests", token.Audiences);

        Assert.True(loginResponse.ExpiresAt > DateTimeOffset.UtcNow);
    }
}