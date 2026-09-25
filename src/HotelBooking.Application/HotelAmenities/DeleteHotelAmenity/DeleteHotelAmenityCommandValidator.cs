using FluentValidation;

namespace HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;

public sealed class DeleteHotelAmenityCommandValidator
    : AbstractValidator<DeleteHotelAmenityCommand>
{
    public DeleteHotelAmenityCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.AmenityId)
            .GreaterThan(0);
    }
}