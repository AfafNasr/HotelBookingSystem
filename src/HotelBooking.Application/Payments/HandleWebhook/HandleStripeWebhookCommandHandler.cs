using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Payments.Gateway;
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
    private readonly IRefundRepository _refundRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;
    private readonly IBookingConfirmationQuery _bookingConfirmationQuery;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public HandleStripeWebhookCommandHandler(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IRefundRepository refundRepository,
        IPaymentGateway paymentGateway,
        IBookingConcurrencyManager bookingConcurrencyManager,
        IBookingConfirmationQuery bookingConfirmationQuery,
        IEmailSender emailSender,
        ILogger<HandleStripeWebhookCommandHandler> logger,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _refundRepository = refundRepository;
        _paymentGateway = paymentGateway;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _bookingConfirmationQuery = bookingConfirmationQuery;
        _emailSender = emailSender;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<HandleStripeWebhookResult> HandleAsync(
        HandleStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (command.EventType != PaymentIntentSucceeded)
        {
            return new HandleStripeWebhookResult(
                true,
                Array.Empty<ApplicationError>());
        }

        if (string.IsNullOrWhiteSpace(
                command.ProviderPaymentIntentId))
        {
            return new HandleStripeWebhookResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "Payment.ProviderPaymentIntentMissing",
                        "The Stripe event does not contain a payment intent ID.",
                        ErrorType.Validation)
                });
        }

        PaymentLog.StripePaymentSucceededEventReceived(
            _logger,
            command.EventId,
            command.ProviderPaymentIntentId);

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
         * All state changes related to this booking are serialized
         * under the same booking-level database lock.
         *
         * This includes:
         * - marking the payment as succeeded
         * - confirming the booking when the hold is still active
         * - creating / completing a refund for a late payment
         *
         * Keeping the refund path inside this critical section prevents
         * two duplicate Stripe webhooks from both observing that no
         * refund exists and attempting to create separate refunds.
         */
        var lockedResult =
            await _bookingConcurrencyManager
                .ExecuteWithBookingLockAsync(
                    bookingId.Value,
                    async ct =>
                    {
                        var outcome =
                            await ProcessSucceededPaymentAsync(
                                command.ProviderPaymentIntentId,
                                bookingId.Value,
                                ct);

                        if (outcome ==
                            PaymentProcessingOutcome.Confirmed)
                        {
                            return new LockedProcessingResult(
                                new HandleStripeWebhookResult(
                                    true,
                                    Array.Empty<ApplicationError>()),
                                ShouldSendConfirmationEmail: true);
                        }

                        if (outcome ==
                            PaymentProcessingOutcome.AlreadyConfirmed)
                        {
                            return new LockedProcessingResult(
                                new HandleStripeWebhookResult(
                                    true,
                                    Array.Empty<ApplicationError>()),
                                ShouldSendConfirmationEmail: false);
                        }

                        var refundResult =
                            await RefundLatePaymentAsync(
                                command.ProviderPaymentIntentId,
                                ct);

                        return new LockedProcessingResult(
                            refundResult,
                            ShouldSendConfirmationEmail: false);
                    },
                    cancellationToken);

        /*
         * Email sending stays outside the database transaction / lock.
         *
         * SMTP or another external email provider can be slow or fail,
         * and we should not hold a SQL row lock while performing
         * unrelated network I/O.
         */
        if (lockedResult.ShouldSendConfirmationEmail)
        {
            try
            {
                await SendBookingConfirmationEmailAsync(
                    bookingId.Value,
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
                    bookingId.Value,
                    exception);
            }
        }

        return lockedResult.Result;
    }

    private async Task<PaymentProcessingOutcome>
        ProcessSucceededPaymentAsync(
            string providerPaymentIntentId,
            int bookingId,
            CancellationToken cancellationToken)
    {
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
            _timeProvider.GetUtcNow().UtcDateTime;

        /*
         * Payment.MarkSucceeded is intentionally idempotent.
         *
         * If Stripe sends the same succeeded event more than once,
         * a previously succeeded payment remains succeeded.
         */
        payment.MarkSucceeded(now);

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
         * The provider says the payment succeeded, but the booking
         * can no longer be confirmed.
         *
         * We persist the successful payment first and then continue
         * to the compensating refund flow.
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

    private async Task<HandleStripeWebhookResult>
        RefundLatePaymentAsync(
            string providerPaymentIntentId,
            CancellationToken cancellationToken)
    {
        var payment =
            await _paymentRepository
                .GetByProviderPaymentIntentIdAsync(
                    providerPaymentIntentId,
                    cancellationToken);

        if (payment is null)
        {
            return new HandleStripeWebhookResult(
                false,
                [PaymentErrors.NotFound]);
        }

        var refund =
            await _refundRepository.GetByPaymentIdAsync(
                payment.Id,
                cancellationToken);

        if (refund is null)
        {
            refund =
                new Refund(
                    payment.Id,
                    payment.Amount,
                    payment.Currency,
                    _timeProvider.GetUtcNow().UtcDateTime);

            _refundRepository.Add(
                refund);

            await _refundRepository.SaveChangesAsync(
                cancellationToken);

            PaymentLog.RefundInitiated(
                _logger,
                refund.Id,
                payment.Id);
        }

        /*
         * Duplicate webhook:
         *
         * If this payment was already refunded successfully,
         * return success without calling the payment provider again.
         */
        if (refund.Status == RefundStatus.Succeeded)
        {
            return new HandleStripeWebhookResult(
                true,
                Array.Empty<ApplicationError>());
        }

        if (refund.Status == RefundStatus.Failed)
        {
            PaymentLog.TerminalRefundFailure(
                _logger,
                refund.Id,
                payment.Id);

            return new HandleStripeWebhookResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "Refund.Failed",
                        "The refund is in a terminal failed state.",
                        ErrorType.Conflict)
                });
        }

        if (payment.ProviderPaymentIntentId is null)
        {
            return new HandleStripeWebhookResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "Payment.ProviderPaymentIntentMissing",
                        "The payment does not have a provider payment intent ID.",
                        ErrorType.Conflict)
                });
        }

        var refundResult =
            await _paymentGateway.CreateRefundAsync(
                new CreateRefundRequest(
                    refund.Id,
                    payment.ProviderPaymentIntentId,
                    refund.Amount),
                cancellationToken);

        refund.MarkSucceeded(
            refundResult.ProviderRefundId,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _refundRepository.SaveChangesAsync(
            cancellationToken);

        PaymentLog.RefundSucceeded(
            _logger,
            refund.Id,
            payment.Id);

        return new HandleStripeWebhookResult(
            true,
            Array.Empty<ApplicationError>());
    }

    private static string GenerateConfirmationNumber()
    {
        return $"HB-{Guid.NewGuid():N}"
            .ToUpperInvariant();
    }

    private async Task SendBookingConfirmationEmailAsync(
        int bookingId,
        CancellationToken cancellationToken)
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

    private enum PaymentProcessingOutcome
    {
        AlreadyConfirmed,
        Confirmed,
        RefundRequired
    }

    private sealed record LockedProcessingResult(
        HandleStripeWebhookResult Result,
        bool ShouldSendConfirmationEmail);
}

public sealed record HandleStripeWebhookResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);