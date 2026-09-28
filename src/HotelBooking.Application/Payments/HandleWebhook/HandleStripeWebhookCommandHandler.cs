using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed class HandleStripeWebhookCommandHandler
{
    private const string PaymentIntentSucceeded =
        "payment_intent.succeeded";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly LatePaymentRefundService _latePaymentRefundService;
    private readonly BookingConfirmationNotifier _bookingConfirmationNotifier;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public HandleStripeWebhookCommandHandler(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IBookingConcurrencyManager bookingConcurrencyManager,
        LatePaymentRefundService latePaymentRefundService,
        BookingConfirmationNotifier bookingConfirmationNotifier,
        ILogger<HandleStripeWebhookCommandHandler> logger,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _latePaymentRefundService = latePaymentRefundService;
        _bookingConfirmationNotifier = bookingConfirmationNotifier;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<HandleStripeWebhookResult> HandleAsync(
        HandleStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        /*
         * Stripe can send many event types to the same webhook.
         *
         * This handler currently owns only the successful
         * PaymentIntent event. Other events are intentionally ignored.
         */
        if (command.EventType != PaymentIntentSucceeded)
        {
            return Success();
        }

        if (string.IsNullOrWhiteSpace(
                command.ProviderPaymentIntentId))
        {
            return new HandleStripeWebhookResult(
                false,
                [
                    new ApplicationError(
                        "Payment.ProviderPaymentIntentMissing",
                        "The Stripe event does not contain a payment intent ID.",
                        ErrorType.Validation)
                ]);
        }

        PaymentLog.StripePaymentSucceededEventReceived(
            _logger,
            command.EventId,
            command.ProviderPaymentIntentId);

        /*
         * Find the booking before acquiring the booking-level lock.
         *
         * This lookup does not make any state-changing decision.
         * All important state transitions are performed again while
         * holding the booking lock below.
         */
        var bookingId =
            await _paymentRepository
                .GetBookingIdByProviderPaymentIntentIdAsync(
                    command.ProviderPaymentIntentId,
                    cancellationToken);

        if (bookingId is null)
        {
            return new HandleStripeWebhookResult(
                false,
                [PaymentErrors.NotFound]);
        }

        /*
         * Serialize payment/booking state transitions for this booking.
         *
         * No Stripe network call and no email network call happens while
         * this database transaction / booking lock is held.
         */
        var outcome =
            await _bookingConcurrencyManager
                .ExecuteWithBookingLockAsync(
                    bookingId.Value,
                    ct => ProcessSucceededPaymentAsync(
                        command.ProviderPaymentIntentId,
                        bookingId.Value,
                        ct),
                    cancellationToken);

        /*
         * A successful provider payment arrived after the booking
         * could no longer be confirmed.
         *
         * The refund service owns the compensating transaction.
         * It persists a durable Refund Id first, calls Stripe outside
         * the database lock, and then finalizes the local refund state.
         */
        if (outcome ==
            PaymentProcessingOutcome.RefundRequired)
        {
            return await _latePaymentRefundService
                .RefundLatePaymentAsync(
                    bookingId.Value,
                    command.ProviderPaymentIntentId,
                    cancellationToken);
        }

        /*
         * Only the transition from PendingPayment -> Confirmed sends
         * the confirmation email.
         *
         * A duplicate Stripe webhook for an already-confirmed booking
         * must not send another confirmation email.
         */
        if (outcome ==
            PaymentProcessingOutcome.Confirmed)
        {
            await _bookingConfirmationNotifier
                .TrySendAsync(
                    bookingId.Value,
                    cancellationToken);
        }

        return Success();
    }

    private async Task<PaymentProcessingOutcome>
        ProcessSucceededPaymentAsync(
            string providerPaymentIntentId,
            int bookingId,
            CancellationToken cancellationToken)
    {
        /*
         * This method is always executed while holding the
         * booking-level database lock.
         */
        var payment =
            await _paymentRepository
                .GetByProviderPaymentIntentIdAsync(
                    providerPaymentIntentId,
                    cancellationToken);

        if (payment is null)
        {
            throw new InvalidOperationException(
                "The payment disappeared while processing the Stripe webhook.");
        }

        var booking =
            await _bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            throw new InvalidOperationException(
                "The booking disappeared while processing the Stripe webhook.");
        }

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        /*
         * MarkSucceeded is intentionally idempotent.
         *
         * Stripe may deliver the same succeeded webhook more than once.
         */
        payment.MarkSucceeded(now);

        /*
         * Duplicate succeeded webhook.
         *
         * The booking has already been confirmed, so no new confirmation
         * number and no duplicate email should be generated.
         */
        if (booking.Status == BookingStatus.Confirmed)
        {
            await _paymentRepository.SaveChangesAsync(
                cancellationToken);

            PaymentLog.PaymentSucceeded(
                _logger,
                payment.Id,
                booking.Id);

            return PaymentProcessingOutcome.AlreadyConfirmed;
        }

        var holdIsActive =
            booking.Status == BookingStatus.PendingPayment &&
            booking.ExpiresAt is not null &&
            booking.ExpiresAt > now;

        if (holdIsActive)
        {
            var confirmationNumber =
                GenerateConfirmationNumber();

            booking.Confirm(
                confirmationNumber,
                now);

            /*
             * Payment and booking are tracked by the same DbContext,
             * so one SaveChanges persists both transitions inside the
             * surrounding booking transaction.
             */
            await _paymentRepository.SaveChangesAsync(
                cancellationToken);

            PaymentLog.PaymentSucceeded(
                _logger,
                payment.Id,
                booking.Id);

            PaymentLog.BookingConfirmed(
                _logger,
                booking.Id,
                payment.Id);

            return PaymentProcessingOutcome.Confirmed;
        }

        /*
         * Stripe successfully captured the payment, but the reservation
         * hold has expired or the booking is no longer confirmable.
         *
         * Persist the successful payment locally first.
         * The caller will then start the compensating refund workflow.
         */
        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        PaymentLog.PaymentSucceeded(
            _logger,
            payment.Id,
            booking.Id);

        PaymentLog.LatePaymentDetected(
            _logger,
            payment.Id,
            booking.Id);

        return PaymentProcessingOutcome.RefundRequired;
    }

    private static HandleStripeWebhookResult Success()
    {
        return new HandleStripeWebhookResult(
            true,
            Array.Empty<ApplicationError>());
    }

    private static string GenerateConfirmationNumber()
    {
        return $"HB-{Guid.NewGuid():N}"
            .ToUpperInvariant();
    }

    private enum PaymentProcessingOutcome
    {
        AlreadyConfirmed,
        Confirmed,
        RefundRequired
    }
}

public sealed record HandleStripeWebhookResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);