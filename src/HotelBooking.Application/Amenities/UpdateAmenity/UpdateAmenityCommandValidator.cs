using FluentValidation;

namespace HotelBooking.Application.Amenities.UpdateAmenity;

public sealed class UpdateAmenityCommandValidator
    : AbstractValidator<UpdateAmenityCommand>
{
    public UpdateAmenityCommandValidator()
    {
        RuleFor(command => command.AmenityId)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}