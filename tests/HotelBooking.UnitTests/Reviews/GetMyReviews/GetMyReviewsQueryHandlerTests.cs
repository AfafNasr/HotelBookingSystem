using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Reviews.GetMyReviews;
using Moq;

namespace HotelBooking.UnitTests.Reviews.GetMyReviews;

public sealed class GetMyReviewsQueryHandlerTests
{
    private readonly Mock<IGetMyReviewsQuery> _query = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private GetMyReviewsQueryHandler CreateHandler()
    {
        return new GetMyReviewsQueryHandler(
            _query.Object,
            _currentUserService.Object);
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
        Assert.Empty(result.Reviews);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _query.Verify(
            query => query.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnReviews_WhenUserIsAuthenticated()
    {
        // Arrange
        const string userId = "user-1";

        IReadOnlyCollection<MyReview> reviews =
        [
            new MyReview(
                ReviewId: 1,
                BookingId: 10,
                HotelId: 100,
                HotelName: "Test Hotel",
                Rating: 5,
                Comment: "Excellent stay.",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null),

            new MyReview(
                ReviewId: 2,
                BookingId: 20,
                HotelId: 200,
                HotelName: "Another Hotel",
                Rating: 4,
                Comment: "Very good.",
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _query
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(2, result.Reviews.Count);
        Assert.Same(reviews, result.Reviews);

        _query.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenUserHasNoReviews()
    {
        // Arrange
        const string userId = "user-1";

        IReadOnlyCollection<MyReview> reviews =
            Array.Empty<MyReview>();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _query
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Reviews);

        _query.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}