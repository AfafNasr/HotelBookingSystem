using HotelBooking.Application.Deals.GetFeaturedDeals;
using Moq;

namespace HotelBooking.UnitTests.Deals.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            15,
            10,
            30,
            0,
            TimeSpan.Zero);

    private readonly Mock<IFeaturedDealsQuery> _featuredDealsQuery;

    public GetFeaturedDealsQueryHandlerTests()
    {
        _featuredDealsQuery = new Mock<IFeaturedDealsQuery>();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFeaturedDeals()
    {
        // Arrange
        var expectedToday = DateOnly.FromDateTime(
            Now.UtcDateTime);

        IReadOnlyCollection<FeaturedDealItem> expectedDeals =
        [
            new FeaturedDealItem(
                HotelId: 1,
                HotelName: "Grand Hotel",
                CityName: "Ramallah",
                Address: "City Center",
                StarRating: 5,
                DiscountPercentage: 20m,
                OriginalPricePerNight: 200m,
                DiscountedPricePerNight: 160m,
                ThumbnailStorageKey: "hotels/1/main.jpg"),

            new FeaturedDealItem(
                HotelId: 2,
                HotelName: "Royal Hotel",
                CityName: "Bethlehem",
                Address: null,
                StarRating: 4,
                DiscountPercentage: 10m,
                OriginalPricePerNight: 150m,
                DiscountedPricePerNight: 135m,
                ThumbnailStorageKey: null)
        ];

        _featuredDealsQuery
            .Setup(query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDeals);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.Same(
            expectedDeals,
            result);

        Assert.Equal(
            2,
            result.Count);

        _featuredDealsQuery.Verify(
            query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoFeaturedDealsExist()
    {
        // Arrange
        var expectedToday = DateOnly.FromDateTime(
            Now.UtcDateTime);

        IReadOnlyCollection<FeaturedDealItem> expectedDeals =
            Array.Empty<FeaturedDealItem>();

        _featuredDealsQuery
            .Setup(query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDeals);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _featuredDealsQuery.Verify(
            query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseCurrentUtcDateFromTimeProvider()
    {
        // Arrange
        var expectedToday =
            new DateOnly(2026, 9, 15);

        _featuredDealsQuery
            .Setup(query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Array.Empty<FeaturedDealItem>());

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        _featuredDealsQuery.Verify(
            query => query.GetAsync(
                expectedToday,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationTokenToQuery()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var expectedToday = DateOnly.FromDateTime(
            Now.UtcDateTime);

        _featuredDealsQuery
            .Setup(query => query.GetAsync(
                expectedToday,
                5,
                cancellationToken))
            .ReturnsAsync(
                Array.Empty<FeaturedDealItem>());

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(
            cancellationToken);

        // Assert
        _featuredDealsQuery.Verify(
            query => query.GetAsync(
                expectedToday,
                5,
                cancellationToken),
            Times.Once);
    }

    private GetFeaturedDealsQueryHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(Now);

        return new GetFeaturedDealsQueryHandler(
            _featuredDealsQuery.Object,
            timeProvider);
    }

    private sealed class TestTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}