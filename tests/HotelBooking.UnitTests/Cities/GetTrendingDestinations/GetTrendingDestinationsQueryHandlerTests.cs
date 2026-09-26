using HotelBooking.Application.Cities.GetTrendingDestinations;
using Moq;

namespace HotelBooking.UnitTests.Cities.GetTrendingDestinations;

public sealed class GetTrendingDestinationsQueryHandlerTests
{
    private readonly Mock<ITrendingDestinationsQuery>
        _trendingDestinationsQuery = new();

    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private readonly TimeProvider _timeProvider =
        new FixedTimeProvider(FixedUtcNow);

    private GetTrendingDestinationsQueryHandler CreateHandler()
    {
        return new GetTrendingDestinationsQueryHandler(
            _trendingDestinationsQuery.Object,
            _timeProvider);
    }

    [Fact]
    public async Task HandleAsync_ShouldRequestTopFiveDestinationsFromLastThirtyDays()
    {
        // Arrange
        var expectedFrom =
            FixedUtcNow.UtcDateTime.AddDays(-30);

        IReadOnlyCollection<TrendingDestination> destinations =
        [
            new TrendingDestination(
                CityId: 1,
                CityName: "Amman",
                ThumbnailStorageKey: "cities/amman.jpg"),

            new TrendingDestination(
                CityId: 2,
                CityName: "Aqaba",
                ThumbnailStorageKey: "cities/aqaba.jpg")
        ];

        _trendingDestinationsQuery
            .Setup(query => query.GetAsync(
                expectedFrom,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinations);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Same(
            destinations,
            result.Destinations);

        _trendingDestinationsQuery.Verify(
            query => query.GetAsync(
                expectedFrom,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoTrendingDestinationsExist()
    {
        // Arrange
        var expectedFrom =
            FixedUtcNow.UtcDateTime.AddDays(-30);

        _trendingDestinationsQuery
            .Setup(query => query.GetAsync(
                expectedFrom,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Array.Empty<TrendingDestination>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Destinations);
        Assert.Empty(result.Errors);

        _trendingDestinationsQuery.Verify(
            query => query.GetAsync(
                expectedFrom,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}