using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Bookings.GetBookingConfirmation;

public sealed class GetBookingConfirmationQueryHandlerTests
{
    private readonly Mock<IBookingConfirmationQuery>
        _bookingConfirmationQuery = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private GetBookingConfirmationQueryHandler CreateHandler()
    {
        return new GetBookingConfirmationQueryHandler(
            _bookingConfirmationQuery.Object,
            _currentUserService.Object);
    }

    private static BookingConfirmation CreateValidConfirmation(
        string userId = "user-1")
    {
        return new BookingConfirmation(
            UserId: userId,
            BookingStatus: BookingStatus.Confirmed,
            ConfirmationNumber: "CONF-12345",
            HotelName: "Test Hotel",
            HotelAddress: "Test Address",
            GuestFullName: "Test User",
            GuestEmail: "test@example.com",
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 12),
            Rooms:
            [
                new BookingConfirmationRoom(
                    RoomId: 1,
                    RoomType: RoomType.Standard,
                    Description: "Standard room",
                    OriginalPricePerNight: 100m)
            ],
            TotalAmount: 200m,
            PaymentStatus: PaymentStatus.Succeeded,
            PaymentAmount: 200m,
            Currency: "USD",
            NumberOfNights: 2,
            SubtotalAmount: 200m,
            DiscountAmount: 0m);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _bookingConfirmationQuery.Verify(
            queryService => queryService.GetAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingConfirmation?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.NotFound",
            error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingBelongsToAnotherUser()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        var confirmation = CreateValidConfirmation(
            userId: "other-user");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.NotFound",
            error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConfirmationNotReady_WhenBookingIsNotConfirmed()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        var confirmation = CreateValidConfirmation() with
        {
            BookingStatus = BookingStatus.PendingPayment
        };

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.ConfirmationNotReady",
            error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConfirmationNotReady_WhenPaymentHasNotSucceeded()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        var confirmation = CreateValidConfirmation() with
        {
            PaymentStatus = null
        };

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.ConfirmationNotReady",
            error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConfirmationNotReady_WhenConfirmationNumberIsMissing()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        var confirmation = CreateValidConfirmation() with
        {
            ConfirmationNumber = null
        };

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Confirmation);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.ConfirmationNotReady",
            error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConfirmation_WhenConfirmationIsReady()
    {
        // Arrange
        var query = new GetBookingConfirmationQuery(
            BookingId: 1);

        var confirmation = CreateValidConfirmation();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingConfirmationQuery
            .Setup(queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(result.Confirmation);
        Assert.Same(
            confirmation,
            result.Confirmation);

        _bookingConfirmationQuery.Verify(
            queryService => queryService.GetAsync(
                query.BookingId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}