using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.UnitTests.Bookings.GetBookingConfirmation;

public sealed class BookingConfirmationEmailBuilderTests
{
    private static BookingConfirmation CreateConfirmation()
    {
        return new BookingConfirmation(
            UserId: "user-1",
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
            TotalAmount: 180m,
            PaymentStatus: PaymentStatus.Succeeded,
            PaymentAmount: 180m,
            Currency: "USD",
            NumberOfNights: 2,
            SubtotalAmount: 200m,
            DiscountAmount: 20m);
    }

    [Fact]
    public void Build_ShouldThrowArgumentNullException_WhenConfirmationIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => BookingConfirmationEmailBuilder.Build(null!));
    }

    [Fact]
    public void Build_ShouldThrowInvalidOperationException_WhenConfirmationNumberIsMissing()
    {
        // Arrange
        var confirmation = CreateConfirmation() with
        {
            ConfirmationNumber = null
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => BookingConfirmationEmailBuilder.Build(
                confirmation));

        // Assert
        Assert.Equal(
            "A confirmed booking must have a confirmation number.",
            exception.Message);
    }

    [Fact]
    public void Build_ShouldCreateEmailWithCorrectRecipientAndSubject()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.Equal(
            confirmation.GuestEmail,
            email.To);

        Assert.Equal(
            "Booking Confirmed - CONF-12345",
            email.Subject);
    }

    [Fact]
    public void Build_ShouldIncludeBookingDetailsInHtmlBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.Contains(
            "CONF-12345",
            email.HtmlBody);

        Assert.Contains(
            "Test Hotel",
            email.HtmlBody);

        Assert.Contains(
            "Test Address",
            email.HtmlBody);

        Assert.Contains(
            "2026-10-10",
            email.HtmlBody);

        Assert.Contains(
            "2026-10-12",
            email.HtmlBody);

        Assert.Contains(
            "2",
            email.HtmlBody);
    }

    [Fact]
    public void Build_ShouldIncludePaymentDetailsInHtmlBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.Contains(
            "200.00 USD",
            email.HtmlBody);

        Assert.Contains(
            "20.00 USD",
            email.HtmlBody);

        Assert.Contains(
            "180.00 USD",
            email.HtmlBody);
    }

    [Fact]
    public void Build_ShouldUseTotalAmount_WhenPaymentAmountIsNull()
    {
        // Arrange
        var confirmation = CreateConfirmation() with
        {
            PaymentAmount = null,
            TotalAmount = 175m
        };

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.Contains(
            "175.00 USD",
            email.HtmlBody);
    }

    [Fact]
    public void Build_ShouldUseNA_WhenHotelAddressIsNull()
    {
        // Arrange
        var confirmation = CreateConfirmation() with
        {
            HotelAddress = null
        };

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.Contains(
            "N/A",
            email.HtmlBody);
    }

    [Fact]
    public void Build_ShouldHtmlEncodeUserControlledValues()
    {
        // Arrange
        var confirmation = CreateConfirmation() with
        {
            GuestFullName = "<script>alert('xss')</script>",
            HotelName = "<b>Dangerous Hotel</b>",
            HotelAddress = "<img src=x onerror=alert(1)>"
        };

        // Act
        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        // Assert
        Assert.DoesNotContain(
            "<script>",
            email.HtmlBody);

        Assert.DoesNotContain(
            "<b>Dangerous Hotel</b>",
            email.HtmlBody);

        Assert.DoesNotContain(
            "<img src=x onerror=alert(1)>",
            email.HtmlBody);

        Assert.Contains(
            "&lt;script&gt;",
            email.HtmlBody);

        Assert.Contains(
            "&lt;b&gt;Dangerous Hotel&lt;/b&gt;",
            email.HtmlBody);

        Assert.Contains(
            "&lt;img src=x onerror=alert(1)&gt;",
            email.HtmlBody);
    }
}