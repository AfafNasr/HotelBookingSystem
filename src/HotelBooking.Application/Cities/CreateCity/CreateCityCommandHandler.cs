using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Countries;
using HotelBooking.Domain.Cities;

namespace HotelBooking.Application.Cities.CreateCity;

public sealed class CreateCityCommandHandler
{
    private readonly IValidator<CreateCityCommand> _validator;
    private readonly ICountryRepository _countryRepository;
    private readonly ICityRepository _cityRepository;

    public CreateCityCommandHandler(
        IValidator<CreateCityCommand> validator,
        ICountryRepository countryRepository,
        ICityRepository cityRepository)
    {
        _validator = validator;
        _countryRepository = countryRepository;
        _cityRepository = cityRepository;
    }

    public async Task<CreateCityResult> HandleAsync(
        CreateCityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CreateCityResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var normalizedName = command.Name.Trim();
        var normalizedCountryCode = command.CountryCode
            .Trim()
            .ToUpperInvariant();

        var countryExists = await _countryRepository.ExistsAsync(
            normalizedCountryCode,
            cancellationToken);

        if (!countryExists)
        {
            return new CreateCityResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "CountryNotFound",
                        "The specified country does not exist.",
                        ErrorType.Validation)
                });
        }

        var existingCity = await _cityRepository.GetByNameAndCountryAsync(
            normalizedName,
            normalizedCountryCode,
            cancellationToken);

        if (existingCity is not null)
        {
            var description = existingCity.IsDeleted
                ? "An archived city with the same name already exists in this country."
                : "A city with the same name already exists in this country.";

            return new CreateCityResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "CityAlreadyExists",
                        description,
                        ErrorType.Conflict)
                });
        }

        var city = new City(
            normalizedName,
            normalizedCountryCode,
            command.PostOffice,
            DateTime.UtcNow);

        _cityRepository.Add(city);

        await _cityRepository.SaveChangesAsync(cancellationToken);

        return new CreateCityResult(
            true,
            city.Id,
            Array.Empty<ApplicationError>());
    }
}