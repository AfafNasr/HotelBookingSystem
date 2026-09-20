using FluentValidation;

namespace HotelBooking.Application.HotelAmenities.AddHotelAmenity;

public sealed class AddHotelAmenityCommandValidator
    : AbstractValidator<AddHotelAmenityCommand>
{
    public AddHotelAmenityCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.AmenityId)
            .GreaterThan(0);
    }
}