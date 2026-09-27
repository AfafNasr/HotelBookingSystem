using FluentValidation;

namespace HotelBooking.Application.Bookings.CancelBooking;

public sealed class CancelBookingCommandValidator
    : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .GreaterThan(0);
    }
}