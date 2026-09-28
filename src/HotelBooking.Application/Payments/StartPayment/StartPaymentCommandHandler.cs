using FluentValidation;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;

namespace HotelBooking.Application.Payments.StartPayment;

public sealed class StartPaymentCommandHandler
{
    private const string Currency = "usd";

    private readonly IValidator<StartPaymentCommand> _validator;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly TimeProvider _timeProvider;

    public StartPaymentCommandHandler(
        IValidator<StartPaymentCommand> validator,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        ICurrentUserService currentUserService,
        IBookingConcurrencyManager bookingConcurrencyManager,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _currentUserService = currentUserService;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _timeProvider = timeProvider;
    }

    public async Task<StartPaymentResult> HandleAsync(
        StartPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                validationResult.ToApplicationErrors());
        }

        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [AuthenticationErrors.Required]);
        }

        var preparation =
           await _bookingConcurrencyManager
               .ExecuteWithBookingLockAsync(
                   command.BookingId,
                   ct => PreparePaymentAsync(
                       command.BookingId,
                       userId,
                       ct),
                   cancellationToken);

        if (!preparation.Succeeded)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                preparation.Errors);
        }

        CreatePaymentIntentResult paymentIntent;

        if (preparation.ProviderPaymentIntentId is not null)
        {
            paymentIntent =
                await _paymentGateway.GetPaymentIntentAsync(
                    preparation.ProviderPaymentIntentId,
                    cancellationToken);
        }
        else
        {
            paymentIntent =
                await _paymentGateway.CreatePaymentIntentAsync(
                    new CreatePaymentIntentRequest(
                        preparation.Amount,
                        preparation.Currency!,
                        preparation.PaymentId!.Value,
                        command.BookingId),
                    cancellationToken);
        }

        return await _bookingConcurrencyManager
            .ExecuteWithBookingLockAsync(
                command.BookingId,
                ct => FinalizePaymentAsync(
                    command.BookingId,
                    userId,
                    preparation.PaymentId!.Value,
                    paymentIntent,
                    ct),
                cancellationToken);
    }

    
    private async Task<PaymentPreparationResult> PreparePaymentAsync(
    int bookingId,
    string userId,
    CancellationToken cancellationToken)
    {
        var booking =
            await _bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return PaymentPreparationResult.Failure(
                BookingErrors.NotFound);
        }

        if (booking.UserId != userId)
        {
            return PaymentPreparationResult.Failure(
                BookingErrors.AccessDenied);
        }

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        if (booking.Status != BookingStatus.PendingPayment)
        {
            return PaymentPreparationResult.Failure(
                BookingErrors.NotPendingPayment);
        }

        if (booking.ExpiresAt is null ||
            booking.ExpiresAt <= now)
        {
            return PaymentPreparationResult.Failure(
                BookingErrors.PaymentHoldExpired);
        }

        var payment =
            await _paymentRepository.GetByBookingIdAsync(
                booking.Id,
                cancellationToken);

        if (payment is null)
        {
            payment =
                new Payment(
                    booking.Id,
                    booking.TotalAmount,
                    Currency,
                    now);

            _paymentRepository.Add(payment);

            /*
             * This SaveChanges runs inside the short booking transaction.
             * When ExecuteWithBookingLockAsync returns, that transaction
             * commits, so Payment.Id becomes durable before Stripe is called.
             */
            await _paymentRepository.SaveChangesAsync(
                cancellationToken);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return PaymentPreparationResult.Failure(
                PaymentErrors.NotPending);
        }

        return PaymentPreparationResult.Success(
            payment.Id,
            payment.Amount,
            payment.Currency,
            payment.ProviderPaymentIntentId);
    }

    private async Task<StartPaymentResult> FinalizePaymentAsync(
    int bookingId,
    string userId,
    int paymentId,
    CreatePaymentIntentResult paymentIntent,
    CancellationToken cancellationToken)
    {
        var booking =
            await _bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken);

        if (booking is null)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [BookingErrors.NotFound]);
        }

        if (booking.UserId != userId)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [BookingErrors.AccessDenied]);
        }

        var payment =
            await _paymentRepository.GetByBookingIdAsync(
                bookingId,
                cancellationToken);

        if (payment is null ||
            payment.Id != paymentId)
        {
            throw new InvalidOperationException(
                "The payment disappeared while finalizing the Stripe payment intent.");
        }

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        /*
         * Persist the Stripe intent even if the booking expired while the
         * external Stripe request was running.
         *
         * This keeps the provider intent associated with the local Payment,
         * so a later Stripe webhook can still reconcile or refund it.
         */
        if (payment.ProviderPaymentIntentId is null)
        {
            payment.AttachProviderPaymentIntent(
                paymentIntent.ProviderPaymentIntentId,
                now);

            await _paymentRepository.SaveChangesAsync(
                cancellationToken);
        }
        else if (!string.Equals(
                     payment.ProviderPaymentIntentId,
                     paymentIntent.ProviderPaymentIntentId,
                     StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The payment is already associated with a different Stripe payment intent.");
        }

        /*
         * State may have changed while we were outside the SQL transaction,
         * so validate the booking again before returning the client secret.
         */
        if (booking.Status != BookingStatus.PendingPayment)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [BookingErrors.NotPendingPayment]);
        }

        if (booking.ExpiresAt is null ||
            booking.ExpiresAt <= now)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [BookingErrors.PaymentHoldExpired]);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [PaymentErrors.NotPending]);
        }

        return new StartPaymentResult(
            true,
            payment.Id,
            paymentIntent.ClientSecret,
            []);
    }

    private sealed record PaymentPreparationResult(
    bool Succeeded,
    int? PaymentId,
    decimal Amount,
    string? Currency,
    string? ProviderPaymentIntentId,
    IReadOnlyCollection<ApplicationError> Errors)
    {
        public static PaymentPreparationResult Success(
            int paymentId,
            decimal amount,
            string currency,
            string? providerPaymentIntentId)
        {
            return new PaymentPreparationResult(
                true,
                paymentId,
                amount,
                currency,
                providerPaymentIntentId,
                []);
        }

        public static PaymentPreparationResult Failure(
            ApplicationError error)
        {
            return new PaymentPreparationResult(
                false,
                null,
                0,
                null,
                null,
                [error]);
        }
    }
}

public sealed record StartPaymentCommand(
    int BookingId);

public sealed record StartPaymentResult(
    bool Succeeded,
    int? PaymentId,
    string? ClientSecret,
    IReadOnlyCollection<ApplicationError> Errors);



