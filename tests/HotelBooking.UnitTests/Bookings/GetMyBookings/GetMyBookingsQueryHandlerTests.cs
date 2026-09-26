using HotelBooking.Application.Bookings.GetMyBookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using Moq;

namespace HotelBooking.UnitTests.Bookings.GetMyBookings;

public sealed class GetMyBookingsQueryHandlerTests
{
    private readonly Mock<IMyBookingsQuery> _myBookingsQuery = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private GetMyBookingsQueryHandler CreateHandler()
    {
        return new GetMyBookingsQueryHandler(
            _myBookingsQuery.Object,
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
        Assert.Empty(result.Bookings);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _myBookingsQuery.Verify(
            query => query.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBookings_WhenUserIsAuthenticated()
    {
        // Arrange
        const string userId = "user-1";

        IReadOnlyCollection<MyBooking> bookings =
        [
            new MyBooking(
                BookingId: 1,
                HotelName: "Test Hotel",
                CheckInDate: new DateOnly(2026, 10, 10),
                CheckOutDate: new DateOnly(2026, 10, 12),
                TotalAmount: 200m,
                Status: BookingStatus.Confirmed,
                ConfirmationNumber: "CONF-12345",
                CreatedAt: new DateTime(
                    2026,
                    9,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc)),

            new MyBooking(
                BookingId: 2,
                HotelName: "Another Hotel",
                CheckInDate: new DateOnly(2026, 11, 1),
                CheckOutDate: new DateOnly(2026, 11, 4),
                TotalAmount: 450m,
                Status: BookingStatus.PendingPayment,
                ConfirmationNumber: null,
                CreatedAt: new DateTime(
                    2026,
                    9,
                    21,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc))
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _myBookingsQuery
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            2,
            result.Bookings.Count);

        Assert.Same(
            bookings,
            result.Bookings);

        _myBookingsQuery.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenUserHasNoBookings()
    {
        // Arrange
        const string userId = "user-1";

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _myBookingsQuery
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MyBooking>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Bookings);
        Assert.Empty(result.Errors);

        _myBookingsQuery.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}