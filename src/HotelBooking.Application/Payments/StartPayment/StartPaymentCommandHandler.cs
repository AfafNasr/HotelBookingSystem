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

        /*
         * StartPayment changes state associated with a booking:
         *
         * - reads booking status / expiration
         * - reads or creates Payment
         * - attaches provider payment intent
         *
         * These operations must be serialized for the same booking.
         *
         * Without the booking-level lock, two concurrent requests can
         * both observe payment == null and both attempt to insert a
         * Payment. The unique database constraint protects the data,
         * but the second request then fails with DbUpdateException.
         */
        return await _bookingConcurrencyManager
            .ExecuteWithBookingLockAsync(
                command.BookingId,
                async ct =>
                {
                    return await StartPaymentLockedAsync(
                        command.BookingId,
                        userId,
                        ct);
                },
                cancellationToken);
    }

    private async Task<StartPaymentResult>
        StartPaymentLockedAsync(
            int bookingId,
            string userId,
            CancellationToken cancellationToken)
    {
        /*
         * Important:
         *
         * The booking must be loaded AFTER the lock has been acquired.
         *
         * Loading it before acquiring the lock could leave us making
         * decisions from stale state while another operation modifies
         * the booking.
         */
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

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

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

            _paymentRepository.Add(
                payment);

            /*
             * Save here so Payment.Id exists before creating
             * the provider payment intent.
             *
             * The payment ID is also part of the request sent
             * to the payment gateway.
             */
            await _paymentRepository.SaveChangesAsync(
                cancellationToken);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                [PaymentErrors.NotPending]);
        }

        /*
         * Idempotent application behavior:
         *
         * If this Payment already has a provider intent, reuse it
         * rather than creating another one.
         */
        if (payment.ProviderPaymentIntentId is not null)
        {
            var existingPaymentIntent =
                await _paymentGateway.GetPaymentIntentAsync(
                    payment.ProviderPaymentIntentId,
                    cancellationToken);

            return new StartPaymentResult(
                true,
                payment.Id,
                existingPaymentIntent.ClientSecret,
                []);
        }

        var paymentIntent =
            await _paymentGateway.CreatePaymentIntentAsync(
                new CreatePaymentIntentRequest(
                    payment.Amount,
                    payment.Currency,
                    payment.Id,
                    booking.Id),
                cancellationToken);

        payment.AttachProviderPaymentIntent(
            paymentIntent.ProviderPaymentIntentId,
            now);

        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        return new StartPaymentResult(
            true,
            payment.Id,
            paymentIntent.ClientSecret,
            []);
    }
}

public sealed record StartPaymentCommand(
    int BookingId);

public sealed record StartPaymentResult(
    bool Succeeded,
    int? PaymentId,
    string? ClientSecret,
    IReadOnlyCollection<ApplicationError> Errors);