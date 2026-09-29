using HotelBooking.Domain.Bookings;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class BookingTests
{
    private static readonly DateOnly CheckInDate =
        new(2026, 10, 10);

    private static readonly DateOnly CheckOutDate =
        new(2026, 10, 12);

    private static readonly DateTime CreatedAt =
        new(
            2026,
            9,
            27,
            10,
            0,
            0,
            DateTimeKind.Utc);

    private static readonly DateTime ExpiresAt =
        new(
            2026,
            9,
            27,
            10,
            15,
            0,
            DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreatePendingPaymentBooking()
    {
        // Act
        var booking =
            CreateBooking();

        // Assert
        Assert.Equal("user-123", booking.UserId);
        Assert.Equal(1, booking.HotelId);
        Assert.Equal(CheckInDate, booking.CheckInDate);
        Assert.Equal(CheckOutDate, booking.CheckOutDate);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);

        Assert.Equal(
            ExpiresAt,
            booking.ExpiresAt);

        Assert.Equal(
            "John Doe",
            booking.GuestFullName);

        Assert.Equal(
            "john@test.com",
            booking.GuestEmail);

        Assert.Equal(
            "+970599123456",
            booking.GuestPhoneNumber);

        Assert.Equal(
            "Late arrival",
            booking.SpecialRequests);

        Assert.Equal(
            300m,
            booking.TotalAmount);

        Assert.Equal(
            CreatedAt,
            booking.CreatedAt);

        Assert.Null(booking.UpdatedAt);
        Assert.Null(booking.ConfirmationNumber);
        Assert.Empty(booking.Rooms);
    }

    [Fact]
    public void Constructor_ShouldTrimGuestDetails()
    {
        // Act
        var booking =
            new Booking(
                "user-123",
                1,
                CheckInDate,
                CheckOutDate,
                ExpiresAt,
                "  John Doe  ",
                "  john@test.com  ",
                "  +970599123456  ",
                "  Late arrival  ",
                300m,
                CreatedAt);

        // Assert
        Assert.Equal(
            "John Doe",
            booking.GuestFullName);

        Assert.Equal(
            "john@test.com",
            booking.GuestEmail);

        Assert.Equal(
            "+970599123456",
            booking.GuestPhoneNumber);

        Assert.Equal(
            "Late arrival",
            booking.SpecialRequests);
    }

    [Fact]
    public void Constructor_WhenSpecialRequestsIsWhitespace_ShouldSetItToNull()
    {
        // Act
        var booking =
            new Booking(
                "user-123",
                1,
                CheckInDate,
                CheckOutDate,
                ExpiresAt,
                "John Doe",
                "john@test.com",
                "+970599123456",
                "   ",
                300m,
                CreatedAt);

        // Assert
        Assert.Null(booking.SpecialRequests);
    }

    [Fact]
    public void AddRoom_WhenRoomDoesNotExist_ShouldAddRoom()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        booking.AddRoom(
            10,
            150m);

        // Assert
        var room =
            Assert.Single(booking.Rooms);

        Assert.Equal(
            10,
            room.RoomId);

        Assert.Equal(
            150m,
            room.OriginalPricePerNight);
    }

    [Fact]
    public void AddRoom_WhenSameRoomIsAddedTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.AddRoom(
            10,
            150m);

        // Act
        var action =
            () => booking.AddRoom(
                10,
                150m);

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "The same room cannot be added to a booking more than once.",
            exception.Message);

        Assert.Single(booking.Rooms);
    }

    [Fact]
    public void Expire_WhenPendingPaymentAndHoldHasExpired_ShouldExpireBooking()
    {
        // Arrange
        var booking =
            CreateBooking();

        var expiredAt =
            ExpiresAt.AddMinutes(1);

        // Act
        booking.Expire(
            expiredAt);

        // Assert
        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.Equal(
            expiredAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Expire_WhenCalledExactlyAtExpiresAt_ShouldExpireBooking()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        booking.Expire(
            ExpiresAt);

        // Assert
        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.Equal(
            ExpiresAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Expire_WhenHoldHasNotExpiredYet_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        var beforeExpiration =
            ExpiresAt.AddSeconds(-1);

        // Act
        var action =
            () => booking.Expire(
                beforeExpiration);

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "The booking payment hold has not expired yet.",
            exception.Message);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);
    }

    [Fact]
    public void Expire_WhenBookingIsConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Confirm(
            "CONF-001",
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => booking.Expire(
                ExpiresAt.AddMinutes(1));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending payment booking can expire.",
            exception.Message);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);
    }

    [Fact]
    public void Expire_WhenBookingIsCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Cancel(
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => booking.Expire(
                ExpiresAt.AddMinutes(1));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending payment booking can expire.",
            exception.Message);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenPendingPaymentAndHoldIsValid_ShouldConfirmBooking()
    {
        // Arrange
        var booking =
            CreateBooking();

        var confirmedAt =
            CreatedAt.AddMinutes(5);

        // Act
        booking.Confirm(
            "CONF-001",
            confirmedAt);

        // Assert
        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Equal(
            "CONF-001",
            booking.ConfirmationNumber);

        Assert.Null(
            booking.ExpiresAt);

        Assert.Equal(
            confirmedAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Confirm_ShouldTrimConfirmationNumber()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        booking.Confirm(
            "  CONF-001  ",
            CreatedAt.AddMinutes(5));

        // Assert
        Assert.Equal(
            "CONF-001",
            booking.ConfirmationNumber);
    }

    [Fact]
    public void Confirm_WhenConfirmationNumberIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        var action =
            () => booking.Confirm(
                "   ",
                CreatedAt.AddMinutes(5));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "confirmationNumber",
            exception.ParamName);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenCalledExactlyAtExpiration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        var action =
            () => booking.Confirm(
                "CONF-001",
                ExpiresAt);

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "An expired booking payment hold cannot be confirmed.",
            exception.Message);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenCalledAfterExpiration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        var action =
            () => booking.Confirm(
                "CONF-001",
                ExpiresAt.AddSeconds(1));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);

        Assert.Equal(
            BookingStatus.PendingPayment,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenBookingIsCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Cancel(
            CreatedAt.AddMinutes(2));

        // Act
        var action =
            () => booking.Confirm(
                "CONF-001",
                CreatedAt.AddMinutes(5));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending payment booking can be confirmed.",
            exception.Message);

        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenBookingIsExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Expire(
            ExpiresAt);

        // Act
        var action =
            () => booking.Confirm(
                "CONF-001",
                ExpiresAt.AddMinutes(1));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);
    }

    [Fact]
    public void Confirm_WhenBookingIsAlreadyConfirmed_ShouldBeIdempotent()
    {
        // Arrange
        var booking =
            CreateBooking();

        var originalConfirmedAt =
            CreatedAt.AddMinutes(5);

        booking.Confirm(
            "CONF-001",
            originalConfirmedAt);

        // Act
        booking.Confirm(
            "CONF-999",
            CreatedAt.AddMinutes(7));

        // Assert
        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Equal(
            "CONF-001",
            booking.ConfirmationNumber);

        Assert.Equal(
            originalConfirmedAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Cancel_WhenPendingPayment_ShouldCancelBooking()
    {
        // Arrange
        var booking =
            CreateBooking();

        var cancelledAt =
            CreatedAt.AddMinutes(5);

        // Act
        booking.Cancel(
            cancelledAt);

        // Assert
        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.Null(
            booking.ExpiresAt);

        Assert.Equal(
            cancelledAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldBeIdempotent()
    {
        // Arrange
        var booking =
            CreateBooking();

        var originalCancelledAt =
            CreatedAt.AddMinutes(5);

        booking.Cancel(
            originalCancelledAt);

        // Act
        booking.Cancel(
            CreatedAt.AddMinutes(10));

        // Assert
        Assert.Equal(
            BookingStatus.Cancelled,
            booking.Status);

        Assert.Equal(
            originalCancelledAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Confirm(
            "CONF-001",
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => booking.Cancel(
                CreatedAt.AddMinutes(6));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending payment booking can be cancelled.",
            exception.Message);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);
    }

    [Fact]
    public void Cancel_WhenExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Expire(
            ExpiresAt);

        // Act
        var action =
            () => booking.Cancel(
                ExpiresAt.AddMinutes(1));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);
    }

    [Fact]
    public void UpdateGuestDetails_WhenPendingPayment_ShouldUpdateGuestDetails()
    {
        // Arrange
        var booking =
            CreateBooking();

        var updatedAt =
            CreatedAt.AddMinutes(3);

        // Act
        booking.UpdateGuestDetails(
            "  Jane Doe  ",
            "  jane@test.com  ",
            "  +970599999999  ",
            "  Quiet room please  ",
            updatedAt);

        // Assert
        Assert.Equal(
            "Jane Doe",
            booking.GuestFullName);

        Assert.Equal(
            "jane@test.com",
            booking.GuestEmail);

        Assert.Equal(
            "+970599999999",
            booking.GuestPhoneNumber);

        Assert.Equal(
            "Quiet room please",
            booking.SpecialRequests);

        Assert.Equal(
            updatedAt,
            booking.UpdatedAt);
    }

    [Fact]
    public void UpdateGuestDetails_WhenSpecialRequestsIsWhitespace_ShouldSetItToNull()
    {
        // Arrange
        var booking =
            CreateBooking();

        // Act
        booking.UpdateGuestDetails(
            "Jane Doe",
            "jane@test.com",
            "+970599999999",
            "   ",
            CreatedAt.AddMinutes(3));

        // Assert
        Assert.Null(
            booking.SpecialRequests);
    }

    [Fact]
    public void UpdateGuestDetails_WhenConfirmed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Confirm(
            "CONF-001",
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => booking.UpdateGuestDetails(
                "Jane Doe",
                "jane@test.com",
                "+970599999999",
                null,
                CreatedAt.AddMinutes(6));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "Only a pending payment booking can be updated.",
            exception.Message);
    }

    [Fact]
    public void UpdateGuestDetails_WhenCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Cancel(
            CreatedAt.AddMinutes(5));

        // Act
        var action =
            () => booking.UpdateGuestDetails(
                "Jane Doe",
                "jane@test.com",
                "+970599999999",
                null,
                CreatedAt.AddMinutes(6));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);
    }

    [Fact]
    public void UpdateGuestDetails_WhenExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var booking =
            CreateBooking();

        booking.Expire(
            ExpiresAt);

        // Act
        var action =
            () => booking.UpdateGuestDetails(
                "Jane Doe",
                "jane@test.com",
                "+970599999999",
                null,
                ExpiresAt.AddMinutes(1));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);
    }

    private static Booking CreateBooking()
    {
        return new Booking(
            "user-123",
            1,
            CheckInDate,
            CheckOutDate,
            ExpiresAt,
            "John Doe",
            "john@test.com",
            "+970599123456",
            "Late arrival",
            300m,
            CreatedAt);
    }
}