using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Cities.DeleteCity;

public sealed record DeleteCityCommand(
    int CityId);

public sealed class DeleteCityCommandHandler
{
    private readonly IValidator<DeleteCityCommand> _validator;
    private readonly ICityRepository _cityRepository;
    private readonly TimeProvider _timeProvider;

    public DeleteCityCommandHandler(
        IValidator<DeleteCityCommand> validator,
        ICityRepository cityRepository,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _cityRepository = cityRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DeleteCityResult> HandleAsync(
        DeleteCityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteCityResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var city = await _cityRepository.GetByIdAsync(
            command.CityId,
            cancellationToken);

        if (city is null)
        {
            return new DeleteCityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "City.NotFound",
                        "The city was not found.",
                        ErrorType.NotFound)
                });
        }

        var hasHotels = await _cityRepository.HasHotelsAsync(
            command.CityId,
            cancellationToken);

        if (hasHotels)
        {
            return new DeleteCityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "City.HasHotels",
                        "The city cannot be deleted because it has associated hotels.",
                        ErrorType.Conflict)
                });
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        city.Delete(now);

        await _cityRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteCityResult(
            true,
            []);
    }
}

public sealed record DeleteCityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);