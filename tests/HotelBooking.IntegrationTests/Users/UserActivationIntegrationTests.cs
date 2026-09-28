using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Users.GetAllUsers;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Users;

public sealed class UserActivationIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task DeactivateUser_WhenAdminDeactivatesCustomer_ShouldPreventCustomerLogin()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var adminClient =
            factory.CreateClient();

        using var customerClient =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var admin =
            await CreateUserAsync(
                factory,
                $"admin-{unique}",
                $"admin-{unique}@test.com",
                Roles.Admin);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        await AuthenticateAsync(
            adminClient,
            admin.UserName!,
            Password);

        // Verify customer can login before deactivation.
        var loginBeforeDeactivation =
            await customerClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    customer.UserName!,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginBeforeDeactivation.StatusCode);

        // Act
        var deactivateResponse =
            await adminClient.PostAsync(
                $"/api/admin/users/{customer.Id}/deactivate",
                content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        var loginAfterDeactivation =
            await customerClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    customer.UserName!,
                    Password));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginAfterDeactivation.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WhenCustomerIsDeactivated_ShouldStillReturnCustomerAsInactive()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var admin =
            await CreateUserAsync(
                factory,
                $"admin-{unique}",
                $"admin-{unique}@test.com",
                Roles.Admin);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        await AuthenticateAsync(
            client,
            admin.UserName!,
            Password);

        var deactivateResponse =
            await client.PostAsync(
                $"/api/admin/users/{customer.Id}/deactivate",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        // Act
        var response =
            await client.GetAsync(
                "/api/admin/users");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<AdminUserItem[]>();

        Assert.NotNull(users);

        var deactivatedCustomer =
            Assert.Single(
                users,
                user =>
                    user.Id == customer.Id);

        Assert.Equal(
            customer.UserName,
            deactivatedCustomer.UserName);

        Assert.Equal(
            customer.Email,
            deactivatedCustomer.Email);

        Assert.False(
            deactivatedCustomer.IsActive);

        Assert.NotNull(
            deactivatedCustomer.DeactivatedAt);

        Assert.Contains(
            Roles.Customer,
            deactivatedCustomer.Roles);
    }

    [Fact]
    public async Task ActivateUser_WhenInactiveCustomerIsActivated_ShouldAllowCustomerToLoginAgain()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var adminClient =
            factory.CreateClient();

        using var customerClient =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var admin =
            await CreateUserAsync(
                factory,
                $"admin-{unique}",
                $"admin-{unique}@test.com",
                Roles.Admin);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        await AuthenticateAsync(
            adminClient,
            admin.UserName!,
            Password);

        var deactivateResponse =
            await adminClient.PostAsync(
                $"/api/admin/users/{customer.Id}/deactivate",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        var loginWhileInactive =
            await customerClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    customer.UserName!,
                    Password));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginWhileInactive.StatusCode);

        // Act
        var activateResponse =
            await adminClient.PostAsync(
                $"/api/admin/users/{customer.Id}/activate",
                content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            activateResponse.StatusCode);

        var loginAfterActivation =
            await customerClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    customer.UserName!,
                    Password));

        Assert.Equal(
            HttpStatusCode.OK,
            loginAfterActivation.StatusCode);

        var usersResponse =
            await adminClient.GetAsync(
                "/api/admin/users");

        Assert.Equal(
            HttpStatusCode.OK,
            usersResponse.StatusCode);

        var users =
            await usersResponse.Content
                .ReadFromJsonAsync<AdminUserItem[]>();

        Assert.NotNull(users);

        var activatedCustomer =
            Assert.Single(
                users,
                user =>
                    user.Id == customer.Id);

        Assert.True(
            activatedCustomer.IsActive);

        Assert.Null(
            activatedCustomer.DeactivatedAt);
    }

    private static async Task<IdentityUser> CreateUserAsync(
        CustomWebApplicationFactory factory,
        string username,
        string email,
        string role)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<IdentityUser>>();

        var user =
            new IdentityUser
            {
                UserName = username,
                Email = email
            };

        var createResult =
            await userManager.CreateAsync(
                user,
                Password);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                ", ",
                createResult.Errors.Select(
                    error =>
                        error.Description)));

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        Assert.True(
            roleResult.Succeeded,
            string.Join(
                ", ",
                roleResult.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    private static async Task AuthenticateAsync(
        HttpClient client,
        string username,
        string password)
    {
        client.DefaultRequestHeaders.Authorization =
            null;

        var response =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    username,
                    password));

        var responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected login to return 200 OK but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var loginResult =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginResult.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.AccessToken);
    }

    [Fact]
    public async Task DeactivateUser_WhenCustomerAlreadyHasAccessToken_ShouldRejectExistingToken()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var adminClient =
            factory.CreateClient();

        using var customerClient =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var admin =
            await CreateUserAsync(
                factory,
                $"admin-{unique}",
                $"admin-{unique}@test.com",
                Roles.Admin);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        /*
         * Both users authenticate before the customer
         * account is deactivated.
         */
        await AuthenticateAsync(
            adminClient,
            admin.UserName!,
            Password);

        await AuthenticateAsync(
            customerClient,
            customer.UserName!,
            Password);

        /*
         * Verify that the customer's current JWT works
         * before deactivation.
         */
        var beforeDeactivation =
            await customerClient.GetAsync(
                "/api/bookings");

        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            beforeDeactivation.StatusCode);

        // Act
        var deactivateResponse =
            await adminClient.PostAsync(
                $"/api/admin/users/{customer.Id}/deactivate",
                content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            deactivateResponse.StatusCode);

        /*
         * Do NOT login again.
         *
         * customerClient still contains the JWT issued
         * before the account was deactivated.
         */
        var responseUsingExistingToken =
            await customerClient.GetAsync(
                "/api/bookings");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            responseUsingExistingToken.StatusCode);
    }
}