using FluentValidation;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed class UpdateHotelCommandValidator
    : AbstractValidator<UpdateHotelCommand>
{
    public UpdateHotelCommandValidator()
    {
        RuleFor(command => command.HotelId)
            .GreaterThan(0);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.CityId)
            .GreaterThan(0);

        RuleFor(command => command.OwnerId)
            .NotEmpty();

        RuleFor(command => command.StarRating)
            .InclusiveBetween(1, 5);

        RuleFor(command => command.Category)
            .IsInEnum();
        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(command => command.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(command => command.Latitude.HasValue);

        RuleFor(command => command.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(command => command.Longitude.HasValue);

        RuleFor(command => command)
            .Must(command =>
                command.Latitude.HasValue ==
                command.Longitude.HasValue)
            .WithMessage(
                "Latitude and longitude must either both be provided or both be null.");
    }
}