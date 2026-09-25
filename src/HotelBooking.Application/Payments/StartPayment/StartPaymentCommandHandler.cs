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
    private readonly TimeProvider _timeProvider;

    public StartPaymentCommandHandler(
        IValidator<StartPaymentCommand> validator,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<StartPaymentResult> HandleAsync(
        StartPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
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

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new StartPaymentResult(
                false,
                null,
                null,
              [AuthenticationErrors.Required]);
        }

        var booking = await _bookingRepository.GetByIdAsync(
            command.BookingId,
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;

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

        var payment = await _paymentRepository.GetByBookingIdAsync(
            booking.Id,
            cancellationToken);

        if (payment is null)
        {
            payment = new Payment(
                booking.Id,
                booking.TotalAmount,
                Currency,
                now);

            _paymentRepository.Add(payment);

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
                Array.Empty<ApplicationError>());
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
            Array.Empty<ApplicationError>());
    }
}

public sealed record StartPaymentCommand(
    int BookingId);