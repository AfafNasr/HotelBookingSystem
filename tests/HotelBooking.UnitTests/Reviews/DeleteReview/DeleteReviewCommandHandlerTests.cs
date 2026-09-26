using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Reviews;
using HotelBooking.Application.Reviews.DeleteReview;
using HotelBooking.Domain.Reviews;
using Moq;

namespace HotelBooking.UnitTests.Reviews.DeleteReview;

public sealed class DeleteReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private DeleteReviewCommandHandler CreateHandler()
    {
        return new DeleteReviewCommandHandler(
            _reviewRepository.Object,
            _currentUserService.Object);
    }

    private static Review CreateReview()
    {
        return new Review(
            bookingId: 1,
            rating: 5,
            comment: "Excellent stay.",
            createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenReviewIdIsInvalid()
    {
        // Arrange
        var command = new DeleteReviewCommand(
            ReviewId: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Review.InvalidId", error.Code);

        _reviewRepository.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Review>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command = new DeleteReviewCommand(
            ReviewId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Authentication.Required", error.Code);

        _reviewRepository.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Review>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenReviewDoesNotExistForUser()
    {
        // Arrange
        var command = new DeleteReviewCommand(
            ReviewId: 1);

        const string userId = "user-1";

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _reviewRepository
            .Setup(repository => repository.GetByIdForUserAsync(
                command.ReviewId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Review.NotFound", error.Code);

        _reviewRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Review>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteReview_WhenReviewExistsForUser()
    {
        // Arrange
        var command = new DeleteReviewCommand(
            ReviewId: 1);

        const string userId = "user-1";

        var review = CreateReview();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _reviewRepository
            .Setup(repository => repository.GetByIdForUserAsync(
                command.ReviewId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _reviewRepository.Verify(
            repository => repository.Remove(review),
            Times.Once);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}