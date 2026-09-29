using System.Reflection;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.CancelBooking;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using Moq;

namespace HotelBooking.UnitTests.Bookings.CancelBooking;

public sealed class CancelBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();

    private readonly Mock<IBookingConcurrencyManager>
        _bookingConcurrencyManager = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly CancelBookingCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            27,
            10,
            0,
            0,
            TimeSpan.Zero);

    private CancelBookingCommandHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(_now);

        /*
         * Unit tests do not test SQL Server locking itself.
         *
         * The real IBookingConcurrencyManager implementation is tested
         * later through integration tests against SQL Server.
         *
         * Here the mock simply executes the protected operation.
         */
        _bookingConcurrencyManager
            .Setup(manager =>
                manager.ExecuteWithBookingLockAsync(
                    It.IsAny<int>(),
                    It.IsAny<
                        Func<
                            CancellationToken,
                            Task<CancelBookingResult>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                (
                    int _,
                    Func<
                        CancellationToken,
                        Task<CancelBookingResult>> operation,
                    CancellationToken cancellationToken) =>
                        operation(cancellationToken));

        return new CancelBookingCommandHandler(
            _validator,
            _bookingRepository.Object,
            _bookingConcurrencyManager.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private Booking CreatePendingBooking(
        string userId = "user-1",
        int bookingId = 1,
        DateTime? expiresAt = null)
    {
        var booking = new Booking(
            userId,
            hotelId: 1,
            checkInDate: new DateOnly(2026, 10, 10),
            checkOutDate: new DateOnly(2026, 10, 12),
            expiresAt:
                expiresAt ??
                _now.UtcDateTime.AddMinutes(15),
            guestFullName: "Test User",
            guestEmail: "test@example.com",
            guestPhoneNumber: "0590000000",
            specialRequests: null,
            totalAmount: 200m,
            createdAt: _now.UtcDateTime);

        SetEntityId(
            booking,
            bookingId);

        return booking;
    }

    private static void SetEntityId<T>(
        T entity,
        int id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var field =
            typeof(T).GetField(
                "<Id>k__BackingField",
                BindingFlags.Instance |
                BindingFlags.NonPublic);

        if (field is null)
        {
            throw new InvalidOperationException(
                $"Could not find the Id backing field on " +
                $"{typeof(T).FullName}.");
        }

        field.SetValue(
            entity,
            id);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenBookingIdIsInvalid()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 0);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _bookingConcurrencyManager.Verify(
            manager =>
                manager.ExecuteWithBookingLockAsync(
                    It.IsAny<int>(),
                    It.IsAny<
                        Func<
                            CancellationToken,
                            Task<CancelBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _bookingConcurrencyManager.Verify(
            manager =>
                manager.ExecuteWithBookingLockAsync(
                    It.IsAny<int>(),
                    It.IsAny<
                        Func<
                            CancellationToken,
                            Task<CancelBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.NotFound",
            error.Code);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAccessDenied_WhenBookingBelongsToAnotherUser()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        var booking =
            CreatePendingBooking(
                userId: "another-user");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.AccessDenied",
            error.Code);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelPendingBooking_WhenBookingBelongsToCurrentUser()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        var booking =
            CreatePendingBooking();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.Null(
            booking.ExpiresAt);

        Assert.Equal(
            _now.UtcDateTime,
            booking.UpdatedAt);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _bookingConcurrencyManager.Verify(
            manager =>
                manager.ExecuteWithBookingLockAsync(
                    command.BookingId,
                    It.IsAny<
                        Func<
                            CancellationToken,
                            Task<CancelBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithoutSaving_WhenBookingIsAlreadyCancelled()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        var booking =
            CreatePendingBooking();

        booking.Cancel(
            _now.UtcDateTime.AddMinutes(-1));

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        var previousUpdatedAt =
            booking.UpdatedAt;

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.Equal(
            previousUpdatedAt,
            booking.UpdatedAt);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCannotCancel_WhenBookingIsConfirmed()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        var booking =
            CreatePendingBooking(
                expiresAt:
                    _now.UtcDateTime.AddMinutes(15));

        booking.Confirm(
            "HB-TEST-123",
            _now.UtcDateTime);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.CannotCancel",
            error.Code);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCannotCancel_WhenBookingIsExpired()
    {
        // Arrange
        var command =
            new CancelBookingCommand(
                BookingId: 1);

        var booking =
            CreatePendingBooking(
                expiresAt:
                    _now.UtcDateTime);

        booking.Expire(
            _now.UtcDateTime);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _bookingRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.BookingId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.CannotCancel",
            error.Code);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class TestTimeProvider : TimeProvider
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