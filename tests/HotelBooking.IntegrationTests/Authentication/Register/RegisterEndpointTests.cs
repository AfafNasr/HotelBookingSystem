using System.Net;
using System.Net.Http.Json;
using HotelBooking.Application.Common.Security;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Authentication.Register;

[Collection(DatabaseTestCollection.Name)]
public sealed class RegisterEndpointTests
{
    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldCreateCustomer()
    {
        // Arrange
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new
        {
            username = $"customer-{Guid.NewGuid():N}",
            email = $"customer-{Guid.NewGuid():N}@example.com",
            password = "Customer123!"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert - HTTP behavior
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Assert - database / Identity behavior
        await using var scope = factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByNameAsync(request.username);

        Assert.NotNull(user);
        Assert.Equal(request.email, user.Email);
        Assert.NotEqual(request.password, user.PasswordHash);

        var isCustomer =
            await userManager.IsInRoleAsync(user, Roles.Customer);

        Assert.True(isCustomer);
    }

    [Fact]
    public async Task Register_WhenEmailIsInvalid_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new
        {
            username = "customer1",
            email = "invalid-email",
            password = "Customer123!"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenPasswordIsWeak_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new
        {
            username = "customer1",
            email = "customer1@example.com",
            password = "weak"
        };

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var firstRequest = new
        {
            username = "duplicate-user",
            email = "first@example.com",
            password = "Customer123!"
        };

        var secondRequest = new
        {
            username = "duplicate-user",
            email = "second@example.com",
            password = "Customer123!"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            firstRequest);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var firstRequest = new
        {
            username = "customer1",
            email = "duplicate@example.com",
            password = "Customer123!"
        };

        var secondRequest = new
        {
            username = "customer2",
            email = "duplicate@example.com",
            password = "Customer123!"
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            firstRequest);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}