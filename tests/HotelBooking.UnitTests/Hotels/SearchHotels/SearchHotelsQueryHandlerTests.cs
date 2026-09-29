using HotelBooking.Application.Hotels.SearchHotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.SearchHotels;

public sealed class SearchHotelsQueryHandlerTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private readonly Mock<IHotelSearchQuery> _hotelSearchQuery = new();

    private readonly TimeProvider _timeProvider =
        new FixedTimeProvider(FixedUtcNow);

    private SearchHotelsQueryHandler CreateHandler()
    {
        return new SearchHotelsQueryHandler(
            new SearchHotelsQueryValidator(_timeProvider),
            _hotelSearchQuery.Object,
            _timeProvider);
    }

    private static SearchHotelsQuery CreateValidQuery()
    {
        return new SearchHotelsQuery(
            Destination: "Test City",
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 12),
            Adults: 2,
            Children: 1,
            Rooms: 1,
            Page: 2,
            PageSize: 10);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationErrors_WhenQueryIsInvalid()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            Destination = ""
        };

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Hotels);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);
        Assert.False(result.HasNextPage);

        Assert.Contains(
            result.Errors,
            error => error.Code ==
                nameof(SearchHotelsQuery.Destination));

        _hotelSearchQuery.Verify(
            search => search.SearchAsync(
                It.IsAny<SearchHotelsQuery>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSearchResults_WhenQueryIsValid()
    {
        // Arrange
        var query = CreateValidQuery();

        IReadOnlyCollection<SearchHotelItem> hotels =
        [
            new SearchHotelItem(
                HotelId: 1,
                Name: "Grand Hotel",
                CityName: "Test City",
                StarRating: 5,
                Description: "Luxury hotel",
                StartingPricePerNight: 120m,
                ThumbnailStorageKey: "hotels/1/thumbnail.jpg"),

            new SearchHotelItem(
                HotelId: 2,
                Name: "City Hotel",
                CityName: "Test City",
                StarRating: 4,
                Description: null,
                StartingPricePerNight: 90m,
                ThumbnailStorageKey: null)
        ];

        var page = new SearchHotelsPage(
            Hotels: hotels,
            HasNextPage: true);

        var expectedNow = FixedUtcNow.UtcDateTime;

        _hotelSearchQuery
            .Setup(search => search.SearchAsync(
                query,
                expectedNow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Same(hotels, result.Hotels);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);

        Assert.True(result.HasNextPage);

        _hotelSearchQuery.Verify(
            search => search.SearchAsync(
                query,
                expectedNow,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyHotels_WhenSearchHasNoResults()
    {
        // Arrange
        var query = CreateValidQuery();

        IReadOnlyCollection<SearchHotelItem> hotels =
            Array.Empty<SearchHotelItem>();

        var page = new SearchHotelsPage(
            Hotels: hotels,
            HasNextPage: false);

        var expectedNow = FixedUtcNow.UtcDateTime;

        _hotelSearchQuery
            .Setup(search => search.SearchAsync(
                query,
                expectedNow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Hotels);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);

        Assert.False(result.HasNextPage);

        _hotelSearchQuery.Verify(
            search => search.SearchAsync(
                query,
                expectedNow,
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