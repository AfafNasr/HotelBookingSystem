using FluentValidation;

namespace HotelBooking.Application.Cities.UpdateCity;

public sealed class UpdateCityValidator
    : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityValidator()
    {
        RuleFor(command => command.CityId)
            .GreaterThan(0)
            .WithMessage("City id must be greater than zero.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("City name is required.")
            .Must(name =>
                !string.IsNullOrWhiteSpace(name) &&
                name.Trim().Length <= 150)
            .WithMessage("City name must not exceed 150 characters.");

        RuleFor(command => command.CountryCode)
            .NotEmpty()
            .WithMessage("Country code is required.")
            .Must(countryCode =>
                !string.IsNullOrWhiteSpace(countryCode) &&
                countryCode.Trim().Length == 2)
            .WithMessage("Country code must be exactly 2 characters.");

        RuleFor(command => command.PostOffice)
            .Must(postOffice =>
                string.IsNullOrWhiteSpace(postOffice) ||
                postOffice.Trim().Length <= 200)
            .WithMessage("Post office must not exceed 200 characters.");
    }
}