using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Cities.UpdateCity;

public sealed class UpdateCityCommandHandler
{
    private readonly ICityRepository _cityRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly IValidator<UpdateCityCommand> _validator;

    public UpdateCityCommandHandler(
        ICityRepository cityRepository,
        ICountryRepository countryRepository,
        IValidator<UpdateCityCommand> validator)
    {
        _cityRepository = cityRepository;
        _countryRepository = countryRepository;
        _validator = validator;
    }

    public async Task<UpdateCityResult> HandleAsync(
        UpdateCityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new UpdateCityResult(false, errors);
        }

        var city = await _cityRepository.GetByIdAsync(
            command.CityId,
            cancellationToken);

        if (city is null)
        {
            return new UpdateCityResult(
                false,
                [
                    new ApplicationError(
                        "CityNotFound",
                        "City was not found.",
                        ErrorType.NotFound)
                ]);
        }

        var normalizedName = command.Name.Trim();
        var normalizedCountryCode =
            command.CountryCode.Trim().ToUpperInvariant();

        var countryExists =
            await _countryRepository.ExistsAsync(
                normalizedCountryCode,
                cancellationToken);

        if (!countryExists)
        {
            return new UpdateCityResult(
                false,
                [
                    new ApplicationError(
                        "CountryNotFound",
                        "Country was not found.",
                        ErrorType.Validation)
                ]);
        }

        var duplicateExists =
            await _cityRepository.ExistsWithNameAndCountryAsync(
                normalizedName,
                normalizedCountryCode,
                city.Id,
                cancellationToken);

        if (duplicateExists)
        {
            return new UpdateCityResult(
                false,
                [
                    new ApplicationError(
                        "CityAlreadyExists",
                        "A city with the same name already exists in this country.",
                        ErrorType.Conflict)
                ]);
        }

        city.Update(
            normalizedName,
            normalizedCountryCode,
            command.PostOffice,
            DateTime.UtcNow);

        await _cityRepository.SaveChangesAsync(cancellationToken);

        return new UpdateCityResult(
            true,
            Array.Empty<ApplicationError>());
    }
}