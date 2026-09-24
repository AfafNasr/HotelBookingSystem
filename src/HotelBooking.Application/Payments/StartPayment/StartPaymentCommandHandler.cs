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

    public StartPaymentCommandHandler(
        IValidator<StartPaymentCommand> validator,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _currentUserService = currentUserService;
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
                new[]
                {
                    new ApplicationError(
                        "Authentication.Required",
                        "The authenticated user could not be identified.",
                        ErrorType.Authentication)
                });
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
                new[]
                {
                    new ApplicationError(
                        "Booking.NotFound",
                        "The booking was not found.",
                        ErrorType.NotFound)
                });
        }

        if (booking.UserId != userId)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                new[]
                {
                    new ApplicationError(
                        "Booking.AccessDenied",
                        "You are not allowed to pay for this booking.",
                        ErrorType.Authorization)
                });
        }

        var now = DateTime.UtcNow;

        if (booking.Status != BookingStatus.PendingPayment)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                new[]
                {
                    new ApplicationError(
                        "Booking.NotPendingPayment",
                        "Only a pending payment booking can be paid.",
                        ErrorType.Conflict)
                });
        }

        if (booking.ExpiresAt is null ||
            booking.ExpiresAt <= now)
        {
            return new StartPaymentResult(
                false,
                null,
                null,
                new[]
                {
                    new ApplicationError(
                        "Booking.PaymentHoldExpired",
                        "The booking payment hold has expired.",
                        ErrorType.Conflict)
                });
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
                new[]
                {
                    new ApplicationError(
                        "Payment.NotPending",
                        "The payment is no longer pending.",
                        ErrorType.Conflict)
                });
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
            DateTime.UtcNow);

        await _paymentRepository.SaveChangesAsync(
            cancellationToken);

        return new StartPaymentResult(
            true,
            payment.Id,
            paymentIntent.ClientSecret,
            Array.Empty<ApplicationError>());
    }
}