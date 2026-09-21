using FluentValidation;

namespace HotelBooking.Application.Bookings.CreateBooking;

public sealed class CreateBookingCommandValidator
    : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.RoomIds)
            .NotEmpty()
            .WithMessage("At least one room must be selected.");

        RuleForEach(command => command.RoomIds)
            .GreaterThan(0);

        RuleFor(command => command.RoomIds)
            .Must(roomIds => roomIds.Distinct().Count() == roomIds.Count)
            .WithMessage("The same room cannot be selected more than once.");

        RuleFor(command => command.CheckOutDate)
            .GreaterThan(command => command.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.");

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