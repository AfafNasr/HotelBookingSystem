using FluentValidation;

namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed class CompleteHotelProfileCommandValidator
    : AbstractValidator<CompleteHotelProfileCommand>
{
    public CompleteHotelProfileCommandValidator()
    {
        RuleFor(x => x.HotelId)
            .GreaterThan(0);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage(
                "Latitude and longitude must either both be provided or both be null.");
    }
}