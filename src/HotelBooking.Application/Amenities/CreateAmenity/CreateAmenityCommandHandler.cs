using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Domain.Amenities;

namespace HotelBooking.Application.Amenities.CreateAmenity;

public sealed class CreateAmenityCommandHandler
{
    private readonly IValidator<CreateAmenityCommand> _validator;
    private readonly IAmenityRepository _amenityRepository;
    private readonly TimeProvider _timeProvider;

    public CreateAmenityCommandHandler(
        IValidator<CreateAmenityCommand> validator,
        IAmenityRepository amenityRepository,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _amenityRepository = amenityRepository;
        _timeProvider = timeProvider;
    }

    public async Task<CreateAmenityResult> HandleAsync(
        CreateAmenityCommand command,
        CancellationToken cancellationToken)
    {
       
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CreateAmenityResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var amenityExists =
            await _amenityRepository.ExistsByNameAsync(
                command.Name,
                cancellationToken);

        if (amenityExists)
        {
            return new CreateAmenityResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "AmenityAlreadyExists",
                        "An amenity with the specified name already exists.",
                        ErrorType.Conflict)
                });
        }
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var amenity = new Amenity(
            command.Name,
            now);

        _amenityRepository.Add(amenity);

        await _amenityRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateAmenityResult(
            true,
            amenity.Id,
            Array.Empty<ApplicationError>());
    }
}