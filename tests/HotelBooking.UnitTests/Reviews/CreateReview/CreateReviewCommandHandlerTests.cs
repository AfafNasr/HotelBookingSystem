using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Reviews;
using HotelBooking.Application.Reviews.CreateReview;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Reviews;
using Moq;

namespace HotelBooking.UnitTests.Reviews.CreateReview;

public sealed class CreateReviewCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly CreateReviewCommandValidator _validator = new();

    private CreateReviewCommandHandler CreateHandler()
    {
        return new CreateReviewCommandHandler(
            _validator,
            _bookingRepository.Object,
            _reviewRepository.Object,
            _currentUserService.Object,
            _timeProvider);
    }

    private static CreateReviewCommand CreateValidCommand()
    {
        return new CreateReviewCommand(
            BookingId: 1,
            Rating: 5,
            Comment: "Excellent stay.");
    }

    private static Booking CreateBooking(
        string userId = "user-1",
        DateOnly? checkOutDate = null)
    {
        var now = DateTime.UtcNow;

        return new Booking(
            userId: userId,
            hotelId: 1,
            checkInDate: new DateOnly(2026, 9, 20),
            checkOutDate: checkOutDate ?? new DateOnly(2026, 9, 22),
            expiresAt: now.AddHours(1),
            guestFullName: "Test User",
            guestEmail: "test@example.com",
            guestPhoneNumber: "123456789",
            specialRequests: null,
            totalAmount: 500m,
            createdAt: now);
    }

    private static Booking CreateConfirmedBooking(
        string userId = "user-1",
        DateOnly? checkOutDate = null)
    {
        var booking = CreateBooking(
            userId,
            checkOutDate);

        booking.Confirm(
            confirmationNumber: "CONF-123",
            confirmedAt: DateTime.UtcNow);

        return booking;
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
        Assert.Null(result.ReviewId);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(CreateReviewCommand.Rating));

        _bookingRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.Add(
                It.IsAny<Review>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ReviewId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("BookingNotFound", error.Code);

        _reviewRepository.Verify(
            repository => repository.ExistsForBookingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.Add(
                It.IsAny<Review>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingBelongsToAnotherUser()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreateConfirmedBooking(
            userId: "user-2");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ReviewId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("BookingNotFound", error.Code);

        _reviewRepository.Verify(
            repository => repository.ExistsForBookingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.Add(
                It.IsAny<Review>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenBookingIsNotConfirmed()
    {
        // Arrange
        var command = CreateValidCommand();

        var booking = CreateBooking(
            userId: "user-1");

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ReviewId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "BookingNotEligibleForReview",
            error.Code);

        _reviewRepository.Verify(
            repository => repository.ExistsForBookingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenStayIsNotCompleted()
    {
        // Arrange
        var command = CreateValidCommand();

        var tomorrow = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var booking = CreateConfirmedBooking(
            userId: "user-1",
            checkOutDate: tomorrow);

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ReviewId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("StayNotCompleted", error.Code);

        _reviewRepository.Verify(
            repository => repository.ExistsForBookingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenReviewAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();

        var yesterday = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(-1));

        var booking = CreateConfirmedBooking(
            userId: "user-1",
            checkOutDate: yesterday);

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _reviewRepository
            .Setup(repository => repository.ExistsForBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ReviewId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("ReviewAlreadyExists", error.Code);

        _reviewRepository.Verify(
            repository => repository.Add(
                It.IsAny<Review>()),
            Times.Never);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateReview_WhenBookingIsEligible()
    {
        // Arrange
        var command = CreateValidCommand();

        var yesterday = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(-1));

        var booking = CreateConfirmedBooking(
            userId: "user-1",
            checkOutDate: yesterday);

        _bookingRepository
            .Setup(repository => repository.GetByIdAsync(
                command.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _reviewRepository
            .Setup(repository => repository.ExistsForBookingAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _reviewRepository.Verify(
            repository => repository.Add(
                It.Is<Review>(review =>
                    review.BookingId == booking.Id &&
                    review.Rating == command.Rating &&
                    review.Comment == command.Comment)),
            Times.Once);

        _reviewRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}