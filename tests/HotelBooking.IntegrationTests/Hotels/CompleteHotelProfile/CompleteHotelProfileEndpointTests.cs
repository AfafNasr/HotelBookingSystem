using HotelBooking.Api.Authentication;
using HotelBooking.Api.Hotels.CompleteHotelProfile;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Hotels.CompleteHotelProfile;

[Collection(DatabaseTestCollection.Name)]
public sealed class CompleteHotelProfileEndpointTests
{
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task CompleteProfile_WhenOwnerOwnsHotel_ShouldReturnNoContentAndPersistProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "profile-owner-1",
            "profile-owner-1@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "profile-owner-1",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "A modern hotel located near the city center.",
            "Rafidia, Nablus",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Equal(
            request.Description,
            hotel.Description);

        Assert.Equal(
            request.Address,
            hotel.Address);

        Assert.Equal(
            request.Latitude,
            hotel.Latitude);

        Assert.Equal(
            request.Longitude,
            hotel.Longitude);

        Assert.NotNull(hotel.UpdatedAt);
    }

    [Fact]
    public async Task CompleteProfile_WhenProfileAlreadyExists_ShouldReturnNoContentAndPersistUpdatedProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "profile-owner-update",
            "profile-owner-update@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await SetHotelProfileDirectlyAsync(
            factory,
            hotelId,
            "Old description",
            "Old address",
            31.9000m,
            35.2000m);

        await AuthenticateAsync(
            client,
            "profile-owner-update",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Updated hotel description",
            "Updated hotel address",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Equal(
            request.Description,
            hotel.Description);

        Assert.Equal(
            request.Address,
            hotel.Address);

        Assert.Equal(
            request.Latitude,
            hotel.Latitude);

        Assert.Equal(
            request.Longitude,
            hotel.Longitude);
    }

    [Fact]
    public async Task CompleteProfile_WhenOptionalDetailsAreNull_ShouldReturnNoContentAndClearProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "profile-owner-clear",
            "profile-owner-clear@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await SetHotelProfileDirectlyAsync(
            factory,
            hotelId,
            "Existing description",
            "Existing address",
            32.2211m,
            35.2544m);

        await AuthenticateAsync(
            client,
            "profile-owner-clear",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            null,
            null,
            null,
            null);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);
    }

    [Fact]
    public async Task CompleteProfile_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CompleteHotelProfileRequest(
            "Description",
            "Address",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            "/api/owner/hotels/1/profile",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_WhenUserIsCustomer_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "profile-customer",
            "profile-customer@test.com",
            Roles.Customer);

        await AuthenticateAsync(
            client,
            "profile-customer",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Description",
            "Address",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            "/api/owner/hotels/1/profile",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_WhenHotelBelongsToAnotherOwner_ShouldReturnForbiddenAndNotModifyProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "requesting-profile-owner",
            "requesting-profile-owner@test.com",
            Roles.HotelOwner);

        var actualOwnerId = await CreateUserAsync(
            factory,
            "actual-profile-owner",
            "actual-profile-owner@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            actualOwnerId);

        await AuthenticateAsync(
            client,
            "requesting-profile-owner",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Unauthorized description",
            "Unauthorized address",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);
    }

    [Fact]
    public async Task CompleteProfile_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "profile-owner-not-found",
            "profile-owner-not-found@test.com",
            Roles.HotelOwner);

        await AuthenticateAsync(
            client,
            "profile-owner-not-found",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Description",
            "Address",
            32.2211m,
            35.2544m);

        var response = await client.PutAsJsonAsync(
            "/api/owner/hotels/999999/profile",
            request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_WhenLatitudeIsProvidedWithoutLongitude_ShouldReturnBadRequestAndNotModifyProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "profile-owner-invalid-location",
            "profile-owner-invalid-location@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "profile-owner-invalid-location",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Description",
            "Address",
            32.2211m,
            null);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);
    }

    [Fact]
    public async Task CompleteProfile_WhenCoordinatesAreOutsideAllowedRange_ShouldReturnBadRequestAndNotModifyProfile()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "profile-owner-invalid-range",
            "profile-owner-invalid-range@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "profile-owner-invalid-range",
            TestPassword);

        var request = new CompleteHotelProfileRequest(
            "Description",
            "Address",
            95m,
            200m);

        var response = await client.PutAsJsonAsync(
            $"/api/owner/hotels/{hotelId}/profile",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await verificationDbContext.Hotels
            .AsNoTracking()
            .SingleAsync(hotel => hotel.Id == hotelId);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);
    }

    private static async Task<string> CreateUserAsync(
        CustomWebApplicationFactory factory,
        string username,
        string email,
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

        var createResult = await userManager.CreateAsync(
            user,
            TestPassword);

        Assert.True(
            createResult.Succeeded,
            string.Join(
                ", ",
                createResult.Errors.Select(
                    error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(
            user,
            role);

        Assert.True(roleResult.Succeeded);

        return user.Id;
    }

    private static async Task<int> CreateHotelAsync(
        CustomWebApplicationFactory factory,
        string ownerId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var city = new City(
            $"Profile Test City {Guid.NewGuid()}",
            "PS",
            null,
            DateTime.UtcNow);

        dbContext.Cities.Add(city);

        await dbContext.SaveChangesAsync();

        var hotel = new Hotel(
            $"Profile Test Hotel {Guid.NewGuid()}",
            city.Id,
            ownerId,
            4,
            HotelCategory.Luxury,
            DateTime.UtcNow);

        dbContext.Hotels.Add(hotel);

        await dbContext.SaveChangesAsync();

        return hotel.Id;
    }

    private static async Task SetHotelProfileDirectlyAsync(
        CustomWebApplicationFactory factory,
        int hotelId,
        string? description,
        string? address,
        decimal? latitude,
        decimal? longitude)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var hotel = await dbContext.Hotels
            .SingleAsync(hotel => hotel.Id == hotelId);

        hotel.CompleteProfile(
            description,
            address,
            latitude,
            longitude,
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync();
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
}
