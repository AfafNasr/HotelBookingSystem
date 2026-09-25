using HotelBooking.Api.Authentication;
using HotelBooking.Application.Common.Security;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Users.PromoteToHotelOwner;

[Collection(DatabaseTestCollection.Name)]
public sealed class PromoteToHotelOwnerEndpointTests
{
    [Fact]
    public async Task PromoteToHotelOwner_WhenAdminPromotesCustomer_ShouldAddHotelOwnerRole()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "owner-promotion-admin";
        const string adminPassword = "Password123!";

        string customerId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "owner-promotion-admin@test.com"
            };

            var adminCreateResult =
                await userManager.CreateAsync(admin, adminPassword);

            Assert.True(adminCreateResult.Succeeded);

            var adminRoleResult =
                await userManager.AddToRoleAsync(admin, Roles.Admin);

            Assert.True(adminRoleResult.Succeeded);

            var customer = new IdentityUser
            {
                UserName = "future-hotel-owner",
                Email = "future-hotel-owner@test.com"
            };

            var customerCreateResult =
                await userManager.CreateAsync(
                    customer,
                    "Password123!");

            Assert.True(customerCreateResult.Succeeded);

            var customerRoleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    Roles.Customer);

            Assert.True(customerRoleResult.Succeeded);

            customerId = customer.Id;
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                adminUsername,
                adminPassword));

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

        var response = await client.PostAsync(
            $"/api/admin/users/{customerId}/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationUserManager =
            verificationScope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

        var promotedUser =
            await verificationUserManager.FindByIdAsync(customerId);

        Assert.NotNull(promotedUser);

        Assert.True(
            await verificationUserManager.IsInRoleAsync(
                promotedUser,
                Roles.Customer));

        Assert.True(
            await verificationUserManager.IsInRoleAsync(
                promotedUser,
                Roles.HotelOwner));
    }

    [Fact]
    public async Task PromoteToHotelOwner_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/admin/users/some-user-id/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PromoteToHotelOwner_WhenCustomerDoesNotHavePermission_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "promotion-customer";
        const string password = "Password123!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = username,
                Email = "promotion-customer@test.com"
            };

            var createResult =
                await userManager.CreateAsync(customer, password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    Roles.Customer);

            Assert.True(roleResult.Succeeded);
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password));

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

        var response = await client.PostAsync(
            "/api/admin/users/some-user-id/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PromoteToHotelOwner_WhenTargetUserDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string username = "not-found-admin";
        const string password = "Password123!";

        await CreateAdminAndAuthenticateAsync(
            factory,
            client,
            username,
            password);

        var response = await client.PostAsync(
            "/api/admin/users/non-existing-user/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task PromoteToHotelOwner_WhenTargetIsAlreadyHotelOwner_ShouldReturnConflict()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "already-owner-admin";
        const string adminPassword = "Password123!";

        string customerId;

        await CreateAdminAndAuthenticateAsync(
            factory,
            client,
            adminUsername,
            adminPassword);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var customer = new IdentityUser
            {
                UserName = "existing-hotel-owner",
                Email = "existing-hotel-owner@test.com"
            };

            var createResult =
                await userManager.CreateAsync(
                    customer,
                    "Password123!");

            Assert.True(createResult.Succeeded);

            var customerRoleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    Roles.Customer);

            Assert.True(customerRoleResult.Succeeded);

            var ownerRoleResult =
                await userManager.AddToRoleAsync(
                    customer,
                    Roles.HotelOwner);

            Assert.True(ownerRoleResult.Succeeded);

            customerId = customer.Id;
        }

        var response = await client.PostAsync(
            $"/api/admin/users/{customerId}/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task PromoteToHotelOwner_WhenTargetIsNotCustomer_ShouldReturnBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        const string adminUsername = "not-customer-admin";
        const string adminPassword = "Password123!";

        string adminId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = adminUsername,
                Email = "not-customer-admin@test.com"
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    Roles.Admin);

            Assert.True(roleResult.Succeeded);

            adminId = admin.Id;
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                adminUsername,
                adminPassword));

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

        var response = await client.PostAsync(
            $"/api/admin/users/{adminId}/hotel-owner-role",
            null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private static async Task CreateAdminAndAuthenticateAsync(
        CustomWebApplicationFactory factory,
        HttpClient client,
        string username,
        string password)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = new IdentityUser
            {
                UserName = username,
                Email = $"{username}@test.com"
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    password);

            Assert.True(createResult.Succeeded);

            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    Roles.Admin);

            Assert.True(roleResult.Succeeded);
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password));

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
