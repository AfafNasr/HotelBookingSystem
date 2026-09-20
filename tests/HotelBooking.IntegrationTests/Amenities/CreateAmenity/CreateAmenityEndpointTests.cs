using HotelBooking.Api.Amenities.CreateAmenity;
using HotelBooking.Api.Authentication.Login;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Amenities.CreateAmenity;

[Collection(DatabaseTestCollection.Name)]
public sealed class CreateAmenityEndpointTests
{
    [Fact]
    public async Task CreateAmenity_WhenAdminRequestIsValid_ShouldCreateAmenity()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "create-amenity-admin";
        const string password = "Password123!";

        await CreateUserAsync(
            factory,
            username,
            "create-amenity-admin@test.com",
            password,
            "Admin");

        await AuthenticateAsync(
            client,
            username,
            password);

        var request = new CreateAmenityRequest(
            "Parking");

        var response = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateAmenityResponse>();

        Assert.NotNull(result);
        Assert.True(result.AmenityId > 0);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var amenity = await dbContext.Amenities
            .AsNoTracking()
            .SingleAsync(
                amenity =>
                    amenity.Id == result.AmenityId);

        Assert.Equal("Parking", amenity.Name);
        Assert.False(amenity.IsDeleted);
        Assert.Null(amenity.UpdatedAt);
        Assert.Null(amenity.DeletedAt);
    }

    [Fact]
    public async Task CreateAmenity_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            new CreateAmenityRequest("Parking"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAmenity_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "create-amenity-customer";
        const string password = "Password123!";

        await CreateUserAsync(
            factory,
            username,
            "create-amenity-customer@test.com",
            password,
            "Customer");

        await AuthenticateAsync(
            client,
            username,
            password);

        var response = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            new CreateAmenityRequest("Parking"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAmenity_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "invalid-amenity-admin";
        const string password = "Password123!";

        await CreateUserAsync(
            factory,
            username,
            "invalid-amenity-admin@test.com",
            password,
            "Admin");

        await AuthenticateAsync(
            client,
            username,
            password);

        var response = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            new CreateAmenityRequest(""));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAmenity_WhenNameAlreadyExists_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "duplicate-amenity-admin";
        const string password = "Password123!";

        await CreateUserAsync(
            factory,
            username,
            "duplicate-amenity-admin@test.com",
            password,
            "Admin");

        await AuthenticateAsync(
            client,
            username,
            password);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            new CreateAmenityRequest("Gym"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            "/api/admin/amenities",
            new CreateAmenityRequest("Gym"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var count = await dbContext.Amenities
            .CountAsync(
                amenity => amenity.Name == "Gym");

        Assert.Equal(1, count);
    }

    private static async Task CreateUserAsync(
        CustomWebApplicationFactory factory,
        string username,
        string email,
        string password,
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

        var createResult =
            await userManager.CreateAsync(
                user,
                password);

        Assert.True(createResult.Succeeded);

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        Assert.True(roleResult.Succeeded);
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                username,
                password));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loginResult =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }
}