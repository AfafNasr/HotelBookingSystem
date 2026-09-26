using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Reviews;
using HotelBooking.Application.Reviews.UpdateReview;
using HotelBooking.Domain.Reviews;
using Moq;

namespace HotelBooking.UnitTests.Reviews.UpdateReview;

public sealed class UpdateReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly UpdateReviewCommandValidator _validator = new();

    private UpdateReviewCommandHandler CreateHandler()
    {
        return new UpdateReviewCommandHandler(
            _validator,
            _reviewRepository.Object,
            _currentUserService.Object,
            _timeProvider);
    }

    private static UpdateReviewCommand CreateValidCommand()
    {
        return new UpdateReviewCommand(
            ReviewId: 1,
            Rating: 4,
            Comment: "Updated review.");
    }

    private static Review CreateReview()
    {
        return new Review(
            bookingId: 1,
            rating: 5,
            comment: "Original review.",
            createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenCommandIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Rating = 0
        };

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(UpdateReviewCommand.Rating));

        _reviewRepository.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
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
        var command = CreateValidCommand();

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

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _reviewRepository.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
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
        var command = CreateValidCommand();

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

        Assert.Equal(
            "Review.NotFound",
            error.Code);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateReview_WhenReviewExistsForUser()
    {
        // Arrange
        var command = CreateValidCommand();

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

        Assert.Equal(command.Rating, review.Rating);
        Assert.Equal(command.Comment, review.Comment);
        Assert.NotNull(review.UpdatedAt);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}