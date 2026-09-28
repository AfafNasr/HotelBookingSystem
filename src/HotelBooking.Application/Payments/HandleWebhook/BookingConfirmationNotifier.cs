using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Emails;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed class BookingConfirmationNotifier
{
    private readonly IBookingConfirmationQuery _bookingConfirmationQuery;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<BookingConfirmationNotifier> _logger;

    public BookingConfirmationNotifier(
        IBookingConfirmationQuery bookingConfirmationQuery,
        IEmailSender emailSender,
        ILogger<BookingConfirmationNotifier> logger)
    {
        _bookingConfirmationQuery = bookingConfirmationQuery;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task TrySendAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmation =
                await _bookingConfirmationQuery.GetAsync(
                    bookingId,
                    cancellationToken);

            if (confirmation is null)
            {
                throw new InvalidOperationException(
                    $"Booking confirmation could not be loaded for booking {bookingId}.");
            }

            var email =
                BookingConfirmationEmailBuilder.Build(
                    confirmation);

            await _emailSender.SendAsync(
                email,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            PaymentLog.BookingConfirmationEmailFailed(
                _logger,
                bookingId,
                exception);
        }
    }
}