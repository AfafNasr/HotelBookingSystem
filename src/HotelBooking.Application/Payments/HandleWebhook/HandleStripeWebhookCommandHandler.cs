using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Payments;
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

    public HandleStripeWebhookCommandHandler(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IRefundRepository refundRepository,
        IPaymentGateway paymentGateway,
        IBookingConcurrencyManager bookingConcurrencyManager,
        ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _refundRepository = refundRepository;
        _paymentGateway = paymentGateway;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _logger = logger;
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
            PaymentLog.StripePaymentSucceededEventReceived(
                        _logger,
                        command.EventId,
                        command.ProviderPaymentIntentId);

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

        var bookingId =
    await _paymentRepository
        .GetBookingIdByProviderPaymentIntentIdAsync(
            command.ProviderPaymentIntentId,
            cancellationToken);

        if (bookingId is null)
        {
            return new HandleStripeWebhookResult(
                false,
                new[]
                {
            new ApplicationError(
                "Payment.NotFound",
                "No payment was found for the Stripe payment intent.",
                ErrorType.NotFound)
                });
        }

        var outcome =
            await _bookingConcurrencyManager
                .ExecuteWithBookingLockAsync(
                    bookingId.Value,
                    async ct =>
                        await ProcessSucceededPaymentAsync(
                            command.ProviderPaymentIntentId,
                            bookingId.Value,
                            ct),
                    cancellationToken);

        if (outcome == PaymentProcessingOutcome.Completed)
        {
            return new HandleStripeWebhookResult(
                true,
                Array.Empty<ApplicationError>());
        }

        return await RefundLatePaymentAsync(
            command.ProviderPaymentIntentId,
            cancellationToken);
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

        var now = DateTime.UtcNow;

        payment.MarkSucceeded(now);

        if (booking.Status == BookingStatus.Confirmed)
        {
            await _paymentRepository.SaveChangesAsync(
                cancellationToken);
            PaymentLog.PaymentSucceeded(
               _logger,
               payment.Id,
               booking.Id);


            return PaymentProcessingOutcome.Completed;
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

            return PaymentProcessingOutcome.Completed;
        }

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
                new[]
                {
                    new ApplicationError(
                        "Payment.NotFound",
                        "The payment could not be found while processing the refund.",
                        ErrorType.NotFound)
                });
        }

        var refund =
            await _refundRepository.GetByPaymentIdAsync(
                payment.Id,
                cancellationToken);

        if (refund is null)
        {
            refund = new Refund(
                payment.Id,
                payment.Amount,
                payment.Currency,
                DateTime.UtcNow);

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
            DateTime.UtcNow);

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
        return $"HB-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private enum PaymentProcessingOutcome
    {
        Completed,
        RefundRequired
    }
}