using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.UpdateBooking;

public sealed class UpdateBookingCommandHandler
{
    private readonly IValidator<UpdateBookingCommand> _validator;
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateBookingCommandHandler(
        IValidator<UpdateBookingCommand> validator,
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

    public async Task<UpdateBookingResult> HandleAsync(
        UpdateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateBookingResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new UpdateBookingResult(
                false,
                [AuthenticationErrors.Required]);
        }

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
                        return new UpdateBookingResult(
                            false,
                            [BookingErrors.NotFound]);
                    }

                    if (booking.UserId != userId)
                    {
                        return new UpdateBookingResult(
                            false,
                            [BookingErrors.AccessDenied]);
                    }

                    if (booking.Status != BookingStatus.PendingPayment)
                    {
                        return new UpdateBookingResult(
                            false,
                            [BookingErrors.CannotUpdate]);
                    }

                    var now =
                        _timeProvider
                            .GetUtcNow()
                            .UtcDateTime;

                    booking.UpdateGuestDetails(
                        command.GuestFullName,
                        command.GuestEmail,
                        command.GuestPhoneNumber,
                        command.SpecialRequests,
                        now);

                    await _bookingRepository.SaveChangesAsync(ct);

                    return new UpdateBookingResult(
                        true,
                        []);
                },
                cancellationToken);
    }
}

public sealed record UpdateBookingResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);