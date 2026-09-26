using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.GetRecentlyVisitedHotels;

public sealed class GetRecentlyVisitedHotelsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IRecentlyVisitedHotelsQuery> _recentlyVisitedHotelsQuery = new();

    private GetRecentlyVisitedHotelsQueryHandler CreateHandler()
    {
        return new GetRecentlyVisitedHotelsQueryHandler(
            _currentUserService.Object,
            _recentlyVisitedHotelsQuery.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Hotels);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _recentlyVisitedHotelsQuery.Verify(
            query => query.GetAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRecentlyVisitedHotels_WhenUserIsAuthenticated()
    {
        // Arrange
        const string userId = "user-1";

        IReadOnlyCollection<RecentlyVisitedHotelItem> hotels =
        [
            new RecentlyVisitedHotelItem(
                HotelId: 1,
                Name: "Grand Hotel",
                CityName: "Test City",
                StarRating: 5,
                StartingPricePerNight: 120m,
                ThumbnailStorageKey: "hotels/1/thumbnail.jpg"),

            new RecentlyVisitedHotelItem(
                HotelId: 2,
                Name: "City Hotel",
                CityName: "Another City",
                StarRating: 4,
                StartingPricePerNight: 90m,
                ThumbnailStorageKey: null)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _recentlyVisitedHotelsQuery
            .Setup(query => query.GetAsync(
                userId,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Same(hotels, result.Hotels);
        Assert.Equal(2, result.Hotels.Count);

        _recentlyVisitedHotelsQuery.Verify(
            query => query.GetAsync(
                userId,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenUserHasNoRecentlyVisitedHotels()
    {
        // Arrange
        const string userId = "user-1";

        IReadOnlyCollection<RecentlyVisitedHotelItem> hotels =
            Array.Empty<RecentlyVisitedHotelItem>();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _recentlyVisitedHotelsQuery
            .Setup(query => query.GetAsync(
                userId,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Hotels);

        _recentlyVisitedHotelsQuery.Verify(
            query => query.GetAsync(
                userId,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}