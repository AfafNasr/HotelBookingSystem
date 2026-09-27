using FluentValidation;

namespace HotelBooking.Application.Hotels.DeleteHotelImage;

public sealed class DeleteHotelImageCommandValidator
    : AbstractValidator<DeleteHotelImageCommand>
{
    public DeleteHotelImageCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.ImageId)
            .GreaterThan(0);
    }
}