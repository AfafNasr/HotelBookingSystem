using System.Net;
using HotelBooking.Application.Emails;

namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public static class BookingConfirmationEmailBuilder
{
    public static EmailMessage Build(
        BookingConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var confirmationNumber = confirmation.ConfirmationNumber
            ?? throw new InvalidOperationException(
                "A confirmed booking must have a confirmation number.");

        return new EmailMessage(
            confirmation.GuestEmail,
            $"Booking Confirmed - {confirmationNumber}",
            BuildHtmlBody(confirmation, confirmationNumber));
    }

    private static string BuildHtmlBody(
        BookingConfirmation confirmation,
        string confirmationNumber)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif; color: #222; line-height: 1.6;">
                <h2>Booking Confirmed</h2>

                <p>Dear {Encode(confirmation.GuestFullName)},</p>

                <p>
                    Thank you for your booking. Your reservation has been confirmed
                    and your payment was successful.
                </p>

                <h3>Booking Details</h3>

                <p>
                    <strong>Confirmation Number:</strong>
                    {Encode(confirmationNumber)}
                    <br />

                    <strong>Hotel:</strong>
                    {Encode(confirmation.HotelName)}
                    <br />

                    <strong>Address:</strong>
                    {Encode(confirmation.HotelAddress ?? "N/A")}
                    <br />

                    <strong>Check-in:</strong>
                    {confirmation.CheckInDate:yyyy-MM-dd}
                    <br />

                    <strong>Check-out:</strong>
                    {confirmation.CheckOutDate:yyyy-MM-dd}
                    <br />

                    <strong>Number of Nights:</strong>
                    {confirmation.NumberOfNights}
                </p>

                <h3>Payment Details</h3>

                <p>
                    <strong>Payment Status:</strong>
                    {confirmation.PaymentStatus?.ToString() ?? "N/A"}
                    <br />

                    <strong>Subtotal:</strong>
                    {FormatMoney(
                        confirmation.SubtotalAmount,
                        confirmation.Currency)}
                    <br />

                    <strong>Discount:</strong>
                    {FormatMoney(
                        confirmation.DiscountAmount,
                        confirmation.Currency)}
                    <br />

                    <strong>Total Paid:</strong>
                    {FormatMoney(
                        confirmation.PaymentAmount ?? confirmation.TotalAmount,
                        confirmation.Currency)}
                </p>

                <p>
                    Thank you for choosing Hotel Booking.
                    We look forward to welcoming you.
                </p>

                <p>
                    Best regards,<br />
                    Hotel Booking
                </p>
            </body>
            </html>
            """;
    }

    private static string FormatMoney(
        decimal amount,
        string? currency)
    {
        var formattedCurrency = string.IsNullOrWhiteSpace(currency)
            ? string.Empty
            : $" {Encode(currency)}";

        return $"{amount:0.00}{formattedCurrency}";
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}