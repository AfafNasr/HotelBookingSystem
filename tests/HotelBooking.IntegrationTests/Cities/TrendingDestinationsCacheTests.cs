using System.Net;
using System.Net.Http.Json;
using HotelBooking.Api.Cities;
using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelBooking.IntegrationTests.Cities;

public sealed class TrendingDestinationsCacheTests
{
    [Fact]
    public async Task GetTrending_WhenRequestedTwice_ShouldUseCachedResponseOnSecondRequest()
    {
        // Arrange
        await using var factory =
            new CustomWebApplicationFactory();

        var fakeQuery =
            new CountingTrendingDestinationsQuery(
            [
                new TrendingDestination(
                    1,
                    "Amman",
                    "cities/amman.jpg"),

                new TrendingDestination(
                    2,
                    "Nablus",
                    "cities/nablus.jpg")
            ]);

        await using var cachedFactory =
            factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ITrendingDestinationsQuery>();

                    services.AddSingleton<ITrendingDestinationsQuery>(
                        fakeQuery);
                });
            });

        using var client =
            cachedFactory.CreateClient();

        // Act - first request
        var firstResponse =
            await client.GetAsync(
                "/api/cities/trending");

        // Assert - first request
        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        var firstDestinations =
            await firstResponse.Content
                .ReadFromJsonAsync<TrendingDestinationResponse[]>();

        Assert.NotNull(firstDestinations);
        Assert.Equal(2, firstDestinations.Length);

        Assert.Equal(
            1,
            fakeQuery.CallCount);

        // Act - second request
        var secondResponse =
            await client.GetAsync(
                "/api/cities/trending");

        // Assert - second request
        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);

        var secondDestinations =
            await secondResponse.Content
                .ReadFromJsonAsync<TrendingDestinationResponse[]>();

        Assert.NotNull(secondDestinations);

        Assert.Equal(
            firstDestinations,
            secondDestinations);

        // Most important assertion:
        // underlying query must NOT execute again.
        Assert.Equal(
            1,
            fakeQuery.CallCount);
    }

    private sealed class CountingTrendingDestinationsQuery
        : ITrendingDestinationsQuery
    {
        private readonly IReadOnlyCollection<TrendingDestination>
            _destinations;

        private int _callCount;

        public CountingTrendingDestinationsQuery(
            IReadOnlyCollection<TrendingDestination> destinations)
        {
            _destinations = destinations;
        }

        public int CallCount =>
            Volatile.Read(ref _callCount);

        public Task<IReadOnlyCollection<TrendingDestination>> GetAsync(
            DateTime from,
            int limit,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _callCount);

            return Task.FromResult(
                _destinations);
        }
    }
}