using FluentValidation;

namespace HotelBooking.Application.Hotels.DeleteHotel;

public sealed class DeleteHotelCommandValidator
    : AbstractValidator<DeleteHotelCommand>
{
    public DeleteHotelCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);
    }
}