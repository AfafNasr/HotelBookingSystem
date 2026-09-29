using System.Reflection;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.UpdateBooking;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;
using Moq;

namespace HotelBooking.UnitTests.Bookings.UpdateBooking;

public sealed class UpdateBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();

    private readonly Mock<IBookingConcurrencyManager>
        _bookingConcurrencyManager = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly UpdateBookingCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            27,
            11,
            0,
            0,
            TimeSpan.Zero);

    private UpdateBookingCommandHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(_now);

        /*
         * Unit tests do not test SQL Server locking.
         *
         * The concurrency manager is mocked as a pass-through so
         * the business logic inside the locked delegate actually runs.
         */
        _bookingConcurrencyManager
            .Setup(manager =>
                manager.ExecuteWithBookingLockAsync(
                    It.IsAny<int>(),
                    It.IsAny<
                        Func<
                            CancellationToken,
                            Task<UpdateBookingResult>>>(),
                    It.IsAny<CancellationToken>()))
            .Returns(
                (
                    int _,
                    Func<
                        CancellationToken,
                        Task<UpdateBookingResult>> operation,
                    CancellationToken cancellationToken) =>
                        operation(cancellationToken));

        return new UpdateBookingCommandHandler(
            _validator,
            _bookingRepository.Object,
            _bookingConcurrencyManager.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private static UpdateBookingCommand CreateValidCommand(
        int bookingId = 1)
    {
        return new UpdateBookingCommand(
            bookingId,
            "Updated Guest",
            "updated.guest@example.com",
            "+970599111111",
            "Updated special request");
    }

    private Booking CreatePendingBooking(
        string userId = "user-1",
        int bookingId = 1,
        DateTime? expiresAt = null)
    {
        var booking =
            new Booking(
                userId,
                hotelId: 1,
                checkInDate: new DateOnly(2026, 10, 10),
                checkOutDate: new DateOnly(2026, 10, 12),
                expiresAt:
                    expiresAt ??
                    _now.UtcDateTime.AddMinutes(15),
                guestFullName: "Original Guest",
                guestEmail: "original@example.com",
                guestPhoneNumber: "+970599000000",
                specialRequests: "Original request",
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
            CreateValidCommand(
                bookingId: 0);

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
                            Task<UpdateBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenGuestDataIsInvalid()
    {
        // Arrange
        var command =
            new UpdateBookingCommand(
                BookingId: 1,
                GuestFullName: "",
                GuestEmail: "not-an-email",
                GuestPhoneNumber: "",
                SpecialRequests: null);

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
                            Task<UpdateBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command =
            CreateValidCommand();

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
                            Task<UpdateBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var command =
            CreateValidCommand();

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
            CreateValidCommand();

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
            "Original Guest",
            booking.GuestFullName);

        Assert.Equal(
            "original@example.com",
            booking.GuestEmail);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateGuestDetails_WhenPendingBookingBelongsToCurrentUser()
    {
        // Arrange
        var command =
            CreateValidCommand();

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
            "Updated Guest",
            booking.GuestFullName);

        Assert.Equal(
            "updated.guest@example.com",
            booking.GuestEmail);

        Assert.Equal(
            "+970599111111",
            booking.GuestPhoneNumber);

        Assert.Equal(
            "Updated special request",
            booking.SpecialRequests);

        Assert.Equal(
            _now.UtcDateTime,
            booking.UpdatedAt);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.NotNull(
            booking.ExpiresAt);

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
                            Task<UpdateBookingResult>>>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldNormalizeSpecialRequests_WhenValueIsWhitespace()
    {
        // Arrange
        var command =
            new UpdateBookingCommand(
                BookingId: 1,
                GuestFullName: "Updated Guest",
                GuestEmail: "updated@example.com",
                GuestPhoneNumber: "+970599111111",
                SpecialRequests: "   ");

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

        Assert.Null(
            booking.SpecialRequests);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCannotUpdate_WhenBookingIsConfirmed()
    {
        // Arrange
        var command =
            CreateValidCommand();

        var booking =
            CreatePendingBooking(
                expiresAt:
                    _now.UtcDateTime.AddMinutes(15));

        booking.Confirm(
            "HB-TEST-123",
            _now.UtcDateTime);

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
            "Booking.CannotUpdate",
            error.Code);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Equal(
            "Original Guest",
            booking.GuestFullName);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCannotUpdate_WhenBookingIsCancelled()
    {
        // Arrange
        var command =
            CreateValidCommand();

        var booking =
            CreatePendingBooking();

        booking.Cancel(
            _now.UtcDateTime);

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
            "Booking.CannotUpdate",
            error.Code);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        _bookingRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCannotUpdate_WhenBookingIsExpired()
    {
        // Arrange
        var command =
            CreateValidCommand();

        var booking =
            CreatePendingBooking(
                expiresAt:
                    _now.UtcDateTime);

        booking.Expire(
            _now.UtcDateTime);

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
            "Booking.CannotUpdate",
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