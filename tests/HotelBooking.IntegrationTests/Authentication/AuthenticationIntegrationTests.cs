using HotelBooking.Api.Authentication;
using HotelBooking.Api.Cities;
using HotelBooking.Application.Common.Security;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace HotelBooking.IntegrationTests.Authentication;

public sealed class AuthenticationIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldCreateCustomer()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N");

        var username =
            $"customer-{unique}";

        var email =
            $"customer-{unique}@test.com";

        var request = new
        {
            username,
            email,
            password = Password
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert - HTTP
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        // Assert - persistence / Identity
        await using var scope =
            factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

        var user =
            await userManager.FindByNameAsync(username);

        Assert.NotNull(user);

        Assert.Equal(
            email,
            user.Email);

        Assert.NotEqual(
            Password,
            user.PasswordHash);

        var isCustomer =
            await userManager.IsInRoleAsync(
                user,
                Roles.Customer);

        Assert.True(isCustomer);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturnValidJwt()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N");

        var username =
            $"login-{unique}";

        var email =
            $"login-{unique}@test.com";

        string userId;

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<IdentityUser>>();

            var user = new IdentityUser
            {
                UserName = username,
                Email = email
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    Password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    Roles.Customer);

            Assert.True(roleResult.Succeeded);

            userId = user.Id;
        }

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                username,
                Password));

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.True(
            result.ExpiresAt >
            DateTimeOffset.UtcNow);

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var token =
            tokenHandler.ReadJwtToken(
                result.AccessToken);

        Assert.Equal(
            userId,
            token.Subject);

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Name &&
                claim.Value == username);

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Role &&
                claim.Value == Roles.Customer);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ShouldReturnUnauthorized()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                "unknown-user",
                Password));

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest(
                "Test City",
                "JO",
                null));

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WhenUserIsCustomer_ShouldReturnForbidden()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N");

        var username =
            $"customer-{unique}";

        var email =
            $"customer-{unique}@test.com";

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<IdentityUser>>();

            var user = new IdentityUser
            {
                UserName = username,
                Email = email
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    Password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    Roles.Customer);

            Assert.True(roleResult.Succeeded);
        }

        var loginResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    username,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var login =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.AccessToken);

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/admin/cities",
            new CreateCityRequest(
                "Test City",
                "JO",
                null));

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}