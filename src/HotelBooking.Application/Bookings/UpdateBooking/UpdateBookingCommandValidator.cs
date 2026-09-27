using FluentValidation;

namespace HotelBooking.Application.Bookings.UpdateBooking;

public sealed class UpdateBookingCommandValidator
    : AbstractValidator<UpdateBookingCommand>
{
    public UpdateBookingCommandValidator()
    {
        RuleFor(command => command.BookingId)
            .GreaterThan(0);

        RuleFor(command => command.GuestFullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.GuestEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.GuestPhoneNumber)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(command => command.SpecialRequests)
            .MaximumLength(2000);
    }
}