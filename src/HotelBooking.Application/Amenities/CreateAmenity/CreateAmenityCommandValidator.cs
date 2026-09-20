using FluentValidation;

namespace HotelBooking.Application.Amenities.CreateAmenity;

public sealed class CreateAmenityCommandValidator
    : AbstractValidator<CreateAmenityCommand>
{
    public CreateAmenityCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}