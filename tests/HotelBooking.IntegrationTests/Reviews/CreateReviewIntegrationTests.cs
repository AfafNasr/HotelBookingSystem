using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.Api.Reviews;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Reviews;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Reviews;

public sealed class CreateReviewIntegrationTests
{
    private const string Password = "Password123!";

    // ============================================================
    // 1. Successful review
    // ============================================================

    [Fact]
    public async Task CreateReview_WhenBookingIsConfirmedAndStayCompleted_ShouldCreateReview()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateCompletedConfirmedBookingAsync(
                factory,
                customer.Id,
                scenario);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var request =
            new CreateReviewRequest(
                5,
                "  Excellent stay!  ");

        // Act
        var response =
            await client.PostAsJsonAsync(
                $"/api/bookings/{bookingId}/review",
                request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        // Assert - HTTP
        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created but received " +
            $"{(int)response.StatusCode} {response.StatusCode}." +
            $"{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);

        var result =
            await response.Content
                .ReadFromJsonAsync<CreateReviewResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.ReviewId > 0);

        // Assert - database
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var review =
            await dbContext.Reviews
                .AsNoTracking()
                .SingleAsync(
                    review =>
                        review.Id == result.ReviewId);

        Assert.Equal(
            bookingId,
            review.BookingId);

        Assert.Equal(
            5,
            review.Rating);

        Assert.Equal(
            "Excellent stay!",
            review.Comment);

        Assert.True(
            review.CreatedAt > DateTime.MinValue);
    }

    // ============================================================
    // 2. Pending payment booking is not eligible
    // ============================================================

    [Fact]
    public async Task CreateReview_WhenBookingIsPendingPayment_ShouldReturnConflictAndNotCreateReview()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateCompletedPendingBookingAsync(
                factory,
                customer.Id,
                scenario);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var request =
            new CreateReviewRequest(
                4,
                "Good stay.");

        // Act
        var response =
            await client.PostAsJsonAsync(
                $"/api/bookings/{bookingId}/review",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.False(
            await ReviewExistsAsync(
                factory,
                bookingId));
    }

    // ============================================================
    // 3. Confirmed booking, but stay is not completed yet
    // ============================================================

    [Fact]
    public async Task CreateReview_WhenStayHasNotCompleted_ShouldReturnConflictAndNotCreateReview()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateFutureConfirmedBookingAsync(
                factory,
                customer.Id,
                scenario);

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var request =
            new CreateReviewRequest(
                5,
                "Trying to review too early.");

        // Act
        var response =
            await client.PostAsJsonAsync(
                $"/api/bookings/{bookingId}/review",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        Assert.False(
            await ReviewExistsAsync(
                factory,
                bookingId));
    }

    // ============================================================
    // 4. User cannot review another user's booking
    // ============================================================

    [Fact]
    public async Task CreateReview_WhenBookingBelongsToAnotherUser_ShouldReturnNotFoundAndNotCreateReview()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var bookingOwner =
            await CreateUserAsync(
                factory,
                $"booking-owner-{unique}",
                $"booking-owner-{unique}@test.com",
                Roles.Customer);

        var otherCustomer =
            await CreateUserAsync(
                factory,
                $"other-customer-{unique}",
                $"other-customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateCompletedConfirmedBookingAsync(
                factory,
                bookingOwner.Id,
                scenario);

        await AuthenticateAsync(
            client,
            otherCustomer.UserName!,
            Password);

        var request =
            new CreateReviewRequest(
                1,
                "Should not be allowed.");

        // Act
        var response =
            await client.PostAsJsonAsync(
                $"/api/bookings/{bookingId}/review",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.False(
            await ReviewExistsAsync(
                factory,
                bookingId));
    }

    // ============================================================
    // 5. Duplicate review is forbidden
    // ============================================================

    [Fact]
    public async Task CreateReview_WhenReviewAlreadyExists_ShouldReturnConflictAndNotCreateDuplicate()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var unique =
            Guid.NewGuid().ToString("N");

        var owner =
            await CreateUserAsync(
                factory,
                $"owner-{unique}",
                $"owner-{unique}@test.com",
                Roles.HotelOwner);

        var customer =
            await CreateUserAsync(
                factory,
                $"customer-{unique}",
                $"customer-{unique}@test.com",
                Roles.Customer);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var bookingId =
            await CreateCompletedConfirmedBookingAsync(
                factory,
                customer.Id,
                scenario);

        await AddReviewAsync(
            factory,
            bookingId,
            rating: 5,
            comment: "Original review.");

        await AuthenticateAsync(
            client,
            customer.UserName!,
            Password);

        var request =
            new CreateReviewRequest(
                3,
                "Second review.");

        // Act
        var response =
            await client.PostAsJsonAsync(
                $"/api/bookings/{bookingId}/review",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var reviewCount =
            await dbContext.Reviews
                .AsNoTracking()
                .CountAsync(
                    review =>
                        review.BookingId == bookingId);

        Assert.Equal(
            1,
            reviewCount);
    }

    // ============================================================
    // Booking setup helpers
    // ============================================================

    private static async Task<int>
        CreateCompletedConfirmedBookingAsync(
            CustomWebApplicationFactory factory,
            string userId,
            BookingScenario scenario)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkOutDate =
            DateOnly.FromDateTime(
                now.AddDays(-2));

        var checkInDate =
            checkOutDate.AddDays(-3);

        var createdAt =
            now.AddDays(-10);

        var expiresAt =
            createdAt.AddMinutes(30);

        var confirmedAt =
            createdAt.AddMinutes(5);

        var numberOfNights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var totalAmount =
            scenario.PricePerNight *
            numberOfNights;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                expiresAt,
                "Review Test Customer",
                "review.customer@test.com",
                "+970599000000",
                null,
                totalAmount,
                createdAt);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        booking.Confirm(
            $"CONF-{Guid.NewGuid():N}",
            confirmedAt);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        return booking.Id;
    }

    private static async Task<int>
        CreateCompletedPendingBookingAsync(
            CustomWebApplicationFactory factory,
            string userId,
            BookingScenario scenario)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkOutDate =
            DateOnly.FromDateTime(
                now.AddDays(-2));

        var checkInDate =
            checkOutDate.AddDays(-3);

        var createdAt =
            now.AddDays(-10);

        var numberOfNights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var totalAmount =
            scenario.PricePerNight *
            numberOfNights;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Review Test Customer",
                "review.customer@test.com",
                "+970599000000",
                null,
                totalAmount,
                createdAt);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        return booking.Id;
    }

    private static async Task<int>
        CreateFutureConfirmedBookingAsync(
            CustomWebApplicationFactory factory,
            string userId,
            BookingScenario scenario)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkInDate =
            DateOnly.FromDateTime(
                now.AddDays(5));

        var checkOutDate =
            checkInDate.AddDays(3);

        var createdAt =
            now.AddMinutes(-10);

        var expiresAt =
            now.AddMinutes(30);

        var confirmedAt =
            now.AddMinutes(-5);

        var numberOfNights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var totalAmount =
            scenario.PricePerNight *
            numberOfNights;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                expiresAt,
                "Future Stay Customer",
                "future.customer@test.com",
                "+970599000000",
                null,
                totalAmount,
                createdAt);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        booking.Confirm(
            $"CONF-{Guid.NewGuid():N}",
            confirmedAt);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        return booking.Id;
    }

    // ============================================================
    // Review setup helpers
    // ============================================================

    private static async Task AddReviewAsync(
        CustomWebApplicationFactory factory,
        int bookingId,
        int rating,
        string? comment)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var review =
            new Review(
                bookingId,
                rating,
                comment,
                DateTime.UtcNow);

        dbContext.Reviews.Add(
            review);

        await dbContext.SaveChangesAsync();
    }

    private static async Task<bool>
        ReviewExistsAsync(
            CustomWebApplicationFactory factory,
            int bookingId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Reviews
            .AsNoTracking()
            .AnyAsync(
                review =>
                    review.BookingId == bookingId);
    }

    // ============================================================
    // Identity helpers
    // ============================================================

    private static async Task<IdentityUser>
        CreateUserAsync(
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
}