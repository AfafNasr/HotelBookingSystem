using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed class LatePaymentRefundService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRepository _refundRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly ILogger<LatePaymentRefundService> _logger;
    private readonly TimeProvider _timeProvider;

    public LatePaymentRefundService(
        IPaymentRepository paymentRepository,
        IRefundRepository refundRepository,
        IPaymentGateway paymentGateway,
        IBookingConcurrencyManager bookingConcurrencyManager,
        ILogger<LatePaymentRefundService> logger,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _refundRepository = refundRepository;
        _paymentGateway = paymentGateway;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<HandleStripeWebhookResult> RefundLatePaymentAsync(
        int bookingId,
        string providerPaymentIntentId,
        CancellationToken cancellationToken)
    {
        var preparation =
            await _bookingConcurrencyManager
                .ExecuteWithBookingLockAsync(
                    bookingId,
                    ct => PrepareAsync(
                        providerPaymentIntentId,
                        ct),
                    cancellationToken);

        if (!preparation.RequiresProviderRefund)
        {
            return preparation.Result;
        }

        /*
         * Important:
         *
         * Stripe network I/O is deliberately outside the database
         * transaction / booking lock.
         *
         * Refund.Id is already durable and is used by the gateway
         * as the provider idempotency key.
         */
        var providerRefund =
            await _paymentGateway.CreateRefundAsync(
                new CreateRefundRequest(
                    preparation.RefundId,
                    preparation.ProviderPaymentIntentId!,
                    preparation.Amount),
                cancellationToken);

        return await _bookingConcurrencyManager
            .ExecuteWithBookingLockAsync(
                bookingId,
                ct => FinalizeAsync(
                    preparation,
                    providerRefund.ProviderRefundId,
                    ct),
                cancellationToken);
    }

    private async Task<RefundPreparationResult> PrepareAsync(
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
            return RefundPreparationResult.Failure(
                PaymentErrors.NotFound);
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
                    _timeProvider
                        .GetUtcNow()
                        .UtcDateTime);

            _refundRepository.Add(refund);

            await _refundRepository.SaveChangesAsync(
                cancellationToken);

            PaymentLog.RefundInitiated(
                _logger,
                refund.Id,
                payment.Id);
        }

        if (refund.Status == RefundStatus.Succeeded)
        {
            return RefundPreparationResult.Completed();
        }

        if (refund.Status == RefundStatus.Failed)
        {
            PaymentLog.TerminalRefundFailure(
                _logger,
                refund.Id,
                payment.Id);

            return RefundPreparationResult.Failure(
                new ApplicationError(
                    "Refund.Failed",
                    "The refund is in a terminal failed state.",
                    ErrorType.Conflict));
        }

        if (payment.ProviderPaymentIntentId is null)
        {
            return RefundPreparationResult.Failure(
                new ApplicationError(
                    "Payment.ProviderPaymentIntentMissing",
                    "The payment does not have a provider payment intent ID.",
                    ErrorType.Conflict));
        }

        return RefundPreparationResult.RequiresRefund(
            refund.Id,
            payment.Id,
            payment.ProviderPaymentIntentId,
            refund.Amount);
    }

    private async Task<HandleStripeWebhookResult> FinalizeAsync(
        RefundPreparationResult preparation,
        string providerRefundId,
        CancellationToken cancellationToken)
    {
        var payment =
            await _paymentRepository
                .GetByProviderPaymentIntentIdAsync(
                    preparation.ProviderPaymentIntentId!,
                    cancellationToken);

        if (payment is null ||
            payment.Id != preparation.PaymentId)
        {
            throw new InvalidOperationException(
                "The payment disappeared while finalizing the refund.");
        }

        var refund =
            await _refundRepository.GetByPaymentIdAsync(
                payment.Id,
                cancellationToken);

        if (refund is null ||
            refund.Id != preparation.RefundId)
        {
            throw new InvalidOperationException(
                "The refund disappeared while finalizing the Stripe refund.");
        }

        /*
         * A concurrent duplicate webhook may already have completed
         * this same logical provider refund.
         */
        if (refund.Status == RefundStatus.Succeeded)
        {
            if (!string.Equals(
                    refund.ProviderRefundId,
                    providerRefundId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The refund is already associated with a different provider refund.");
            }

            return Success();
        }

        if (refund.Status == RefundStatus.Failed)
        {
            PaymentLog.TerminalRefundFailure(
                _logger,
                refund.Id,
                payment.Id);

            return new HandleStripeWebhookResult(
                false,
                [
                    new ApplicationError(
                        "Refund.Failed",
                        "The refund is in a terminal failed state.",
                        ErrorType.Conflict)
                ]);
        }

        refund.MarkSucceeded(
            providerRefundId,
            _timeProvider
                .GetUtcNow()
                .UtcDateTime);

        await _refundRepository.SaveChangesAsync(
            cancellationToken);

        PaymentLog.RefundSucceeded(
            _logger,
            refund.Id,
            payment.Id);

        return Success();
    }

    private static HandleStripeWebhookResult Success()
    {
        return new HandleStripeWebhookResult(
            true,
            Array.Empty<ApplicationError>());
    }

    private sealed record RefundPreparationResult(
        bool RequiresProviderRefund,
        int RefundId,
        int PaymentId,
        string? ProviderPaymentIntentId,
        decimal Amount,
        HandleStripeWebhookResult Result)
    {
        public static RefundPreparationResult RequiresRefund(
            int refundId,
            int paymentId,
            string providerPaymentIntentId,
            decimal amount)
        {
            return new RefundPreparationResult(
                true,
                refundId,
                paymentId,
                providerPaymentIntentId,
                amount,
                Success());
        }

        public static RefundPreparationResult Completed()
        {
            return new RefundPreparationResult(
                false,
                0,
                0,
                null,
                0m,
                Success());
        }

        public static RefundPreparationResult Failure(
            ApplicationError error)
        {
            return new RefundPreparationResult(
                false,
                0,
                0,
                null,
                0m,
                new HandleStripeWebhookResult(
                    false,
                    [error]));
        }

        private static HandleStripeWebhookResult Success()
        {
            return new HandleStripeWebhookResult(
                true,
                Array.Empty<ApplicationError>());
        }
    }
}