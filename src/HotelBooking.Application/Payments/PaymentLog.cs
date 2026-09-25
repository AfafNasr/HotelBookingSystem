using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Payments;

internal static class PaymentLog
{
    private static readonly Action<ILogger, string, string, Exception?>
        StripePaymentSucceededEventReceivedMessage =
            LoggerMessage.Define<string, string>(
                LogLevel.Information,
                new EventId(
                    2001,
                    nameof(StripePaymentSucceededEventReceived)),
                "Stripe event {StripeEventId} received for succeeded payment intent {ProviderPaymentIntentId}.");

    private static readonly Action<ILogger, int, int, Exception?>
        PaymentSucceededMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Information,
                new EventId(
                    2002,
                    nameof(PaymentSucceeded)),
                "Payment {PaymentId} succeeded for booking {BookingId}.");

    private static readonly Action<ILogger, int, int, Exception?>
        BookingConfirmedMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Information,
                new EventId(
                    2003,
                    nameof(BookingConfirmed)),
                "Booking {BookingId} was confirmed after successful payment {PaymentId}.");

    private static readonly Action<ILogger, int, int, Exception?>
        LatePaymentDetectedMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Warning,
                new EventId(
                    2004,
                    nameof(LatePaymentDetected)),
                "Late payment {PaymentId} was detected for booking {BookingId}. A refund is required.");

    private static readonly Action<ILogger, int, int, Exception?>
        RefundInitiatedMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Information,
                new EventId(
                    2005,
                    nameof(RefundInitiated)),
                "Refund {RefundId} was initiated for payment {PaymentId}.");

    private static readonly Action<ILogger, int, int, Exception?>
        RefundSucceededMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Information,
                new EventId(
                    2006,
                    nameof(RefundSucceeded)),
                "Refund {RefundId} succeeded for payment {PaymentId}.");

    private static readonly Action<ILogger, int, int, Exception?>
        TerminalRefundFailureMessage =
            LoggerMessage.Define<int, int>(
                LogLevel.Warning,
                new EventId(
                    2007,
                    nameof(TerminalRefundFailure)),
                "Refund {RefundId} for payment {PaymentId} is in a terminal failed state.");

    private static readonly Action<ILogger, int, Exception?>
    BookingConfirmationEmailFailedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(
                2008,
                nameof(BookingConfirmationEmailFailed)),
            "Failed to send confirmation email for booking {BookingId}.");

    public static void StripePaymentSucceededEventReceived(
        ILogger logger,
        string stripeEventId,
        string providerPaymentIntentId)
    {
        StripePaymentSucceededEventReceivedMessage(
            logger,
            stripeEventId,
            providerPaymentIntentId,
            null);
    }

    public static void PaymentSucceeded(
        ILogger logger,
        int paymentId,
        int bookingId)
    {
        PaymentSucceededMessage(
            logger,
            paymentId,
            bookingId,
            null);
    }

    public static void BookingConfirmed(
        ILogger logger,
        int bookingId,
        int paymentId)
    {
        BookingConfirmedMessage(
            logger,
            bookingId,
            paymentId,
            null);
    }

    public static void LatePaymentDetected(
        ILogger logger,
        int paymentId,
        int bookingId)
    {
        LatePaymentDetectedMessage(
            logger,
            paymentId,
            bookingId,
            null);
    }

    public static void RefundInitiated(
        ILogger logger,
        int refundId,
        int paymentId)
    {
        RefundInitiatedMessage(
            logger,
            refundId,
            paymentId,
            null);
    }

    public static void RefundSucceeded(
        ILogger logger,
        int refundId,
        int paymentId)
    {
        RefundSucceededMessage(
            logger,
            refundId,
            paymentId,
            null);
    }

    public static void TerminalRefundFailure(
        ILogger logger,
        int refundId,
        int paymentId)
    {
        TerminalRefundFailureMessage(
            logger,
            refundId,
            paymentId,
            null);
    }

    public static void BookingConfirmationEmailFailed(
    ILogger logger,
    int bookingId,
    Exception exception)
    {
        BookingConfirmationEmailFailedMessage(
            logger,
            bookingId,
            exception);
    }

}