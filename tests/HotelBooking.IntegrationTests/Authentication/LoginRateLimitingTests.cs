using System.Net;
using System.Net.Http.Json;
using HotelBooking.Api.Authentication;
using HotelBooking.IntegrationTests.Infrastructure;

namespace HotelBooking.IntegrationTests.Authentication;

public sealed class LoginRateLimitingTests
{
    [Fact]
    public async Task Login_WhenRateLimitIsExceeded_ShouldReturnTooManyRequests()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var request =
            new LoginRequest(
                "non-existing-user",
                "WrongPassword123!");

        // Act + Assert
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var rateLimitedResponse =
            await client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rateLimitedResponse.StatusCode);
    }
}