using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.CancelBooking;

public sealed class CancelBookingCommandHandler
{
    private readonly IValidator<CancelBookingCommand> _validator;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CancelBookingCommandHandler(
        IValidator<CancelBookingCommand> validator,
        IBookingRepository bookingRepository,
        IBookingConcurrencyManager bookingConcurrencyManager,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _bookingRepository = bookingRepository;
        _bookingConcurrencyManager = bookingConcurrencyManager;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CancelBookingResult> HandleAsync(
        CancelBookingCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CancelBookingResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new CancelBookingResult(
                false,
                [AuthenticationErrors.Required]);
        }

        /*
         * Cancellation competes with:
         *
         * - payment success webhook
         * - booking expiration
         *
         * All three operations affect the same booking state.
         * Therefore they must use the same booking-level lock.
         */
        return await _bookingConcurrencyManager
            .ExecuteWithBookingLockAsync(
                command.BookingId,
                async ct =>
                {
                    var booking =
                        await _bookingRepository.GetByIdAsync(
                            command.BookingId,
                            ct);

                    if (booking is null)
                    {
                        return new CancelBookingResult(
                            false,
                            [BookingErrors.NotFound]);
                    }

                    if (booking.UserId != userId)
                    {
                        return new CancelBookingResult(
                            false,
                            [BookingErrors.AccessDenied]);
                    }

                    /*
                     * Idempotent cancellation.
                     *
                     * A repeated client request should not turn an
                     * already-cancelled booking into an error.
                     */
                    if (booking.Status == BookingStatus.Cancelled)
                    {
                        return new CancelBookingResult(
                            true,
                            []);
                    }

                    if (booking.Status != BookingStatus.PendingPayment)
                    {
                        return new CancelBookingResult(
                            false,
                            [BookingErrors.CannotCancel]);
                    }

                    var now =
                        _timeProvider
                            .GetUtcNow()
                            .UtcDateTime;

                    booking.Cancel(now);

                    await _bookingRepository.SaveChangesAsync(ct);

                    return new CancelBookingResult(
                        true,
                        []);
                },
                cancellationToken);
    }
}

public sealed record CancelBookingResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);