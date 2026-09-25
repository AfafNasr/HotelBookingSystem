using HotelBooking.Api.Authentication;
using HotelBooking.Api.Rooms;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HotelBooking.IntegrationTests.Rooms.CreateRoom;

[Collection(DatabaseTestCollection.Name)]
public sealed class CreateRoomEndpointTests
{
    private const string TestPassword = "Password123!";

    [Fact]
    public async Task CreateRoom_WhenOwnerOwnsHotel_ShouldReturnCreatedAndPersistRoom()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "room-owner-1",
            "room-owner-1@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "room-owner-1",
            TestPassword);

        var request = CreateValidRequest("101");

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotelId}/rooms",
            request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var room = await verificationDbContext.Room
            .AsNoTracking()
            .SingleOrDefaultAsync(room =>
                room.HotelId == hotelId &&
                room.RoomNumber == "101");

        Assert.NotNull(room);
        Assert.Equal(RoomType.Standard, room.RoomType);
        Assert.Equal(2, room.AdultsCapacity);
        Assert.Equal(1, room.ChildrenCapacity);
        Assert.Equal(100m, room.PricePerNight);
    }

    [Fact]
    public async Task CreateRoom_WhenUserIsAdmin_ShouldReturnCreatedAndPersistRoom()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "room-owner-admin-test",
            "room-owner-admin-test@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await CreateUserAsync(
            factory,
            "room-admin",
            "room-admin@test.com",
            Roles.Admin);

        await AuthenticateAsync(
            client,
            "room-admin",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotelId}/rooms",
            CreateValidRequest("201"));

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var exists = await verificationDbContext.Room
            .AsNoTracking()
            .AnyAsync(room =>
                room.HotelId == hotelId &&
                room.RoomNumber == "201");

        Assert.True(exists);
    }

    [Fact]
    public async Task CreateRoom_WhenHotelBelongsToAnotherOwner_ShouldReturnForbiddenAndNotPersistRoom()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "requesting-room-owner",
            "requesting-room-owner@test.com",
            Roles.HotelOwner);

        var actualOwnerId = await CreateUserAsync(
            factory,
            "actual-room-owner",
            "actual-room-owner@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            actualOwnerId);

        await AuthenticateAsync(
            client,
            "requesting-room-owner",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotelId}/rooms",
            CreateValidRequest("301"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var exists = await verificationDbContext.Room
            .AsNoTracking()
            .AnyAsync(room =>
                room.HotelId == hotelId &&
                room.RoomNumber == "301");

        Assert.False(exists);
    }

    [Fact]
    public async Task CreateRoom_WhenUserIsCustomer_ShouldReturnForbidden()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "room-customer",
            "room-customer@test.com",
            Roles.Customer);

        await AuthenticateAsync(
            client,
            "room-customer",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/hotels/1/rooms",
            CreateValidRequest("401"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WhenUserIsUnauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/hotels/1/rooms",
            CreateValidRequest("501"));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        await CreateUserAsync(
            factory,
            "room-owner-hotel-not-found",
            "room-owner-hotel-not-found@test.com",
            Roles.HotelOwner);

        await AuthenticateAsync(
            client,
            "room-owner-hotel-not-found",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            "/api/hotels/999999/rooms",
            CreateValidRequest("601"));

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WhenRoomNumberAlreadyExistsInHotel_ShouldReturnConflictAndKeepSingleRoom()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "room-owner-duplicate",
            "room-owner-duplicate@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AddRoomDirectlyAsync(
            factory,
            hotelId,
            "701");

        await AuthenticateAsync(
            client,
            "room-owner-duplicate",
            TestPassword);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotelId}/rooms",
            CreateValidRequest("701"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var count = await verificationDbContext.Room
            .AsNoTracking()
            .CountAsync(room =>
                room.HotelId == hotelId &&
                room.RoomNumber == "701");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateRoom_WhenRequestIsInvalid_ShouldReturnBadRequestAndNotPersistRoom()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var ownerId = await CreateUserAsync(
            factory,
            "room-owner-invalid",
            "room-owner-invalid@test.com",
            Roles.HotelOwner);

        var hotelId = await CreateHotelAsync(
            factory,
            ownerId);

        await AuthenticateAsync(
            client,
            "room-owner-invalid",
            TestPassword);

        var request = new SaveRoomRequest(
            "",
            RoomType.Standard,
            "Invalid room.",
            0,
            -1,
            0m);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotelId}/rooms",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var count = await verificationDbContext.Room
            .AsNoTracking()
            .CountAsync(room => room.HotelId == hotelId);

        Assert.Equal(0, count);
    }

    private static SaveRoomRequest CreateValidRequest(
        string roomNumber)
    {
        return new SaveRoomRequest(
            roomNumber,
            RoomType.Standard,
            "Comfortable test room.",
            2,
            1,
            100m);
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
            $"Room Test City {Guid.NewGuid()}",
            "PS",
            null,
            DateTime.UtcNow);

        dbContext.Cities.Add(city);

        await dbContext.SaveChangesAsync();

        var hotel = new Hotel(
            $"Room Test Hotel {Guid.NewGuid()}",
            city.Id,
            ownerId,
            4,
            HotelCategory.Luxury,
            DateTime.UtcNow);

        dbContext.Hotels.Add(hotel);

        await dbContext.SaveChangesAsync();

        return hotel.Id;
    }

    private static async Task AddRoomDirectlyAsync(
        CustomWebApplicationFactory factory,
        int hotelId,
        string roomNumber)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var room = new Room(
            hotelId,
            roomNumber,
            RoomType.Standard,
            "Existing test room.",
            2,
            1,
            100m,
            DateTime.UtcNow);

        dbContext.Room.Add(room);

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
