using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Amenities.UpdateAmenity;

public sealed class UpdateAmenityCommandHandler
{
    private readonly IValidator<UpdateAmenityCommand> _validator;
    private readonly IAmenityRepository _amenityRepository;
    private readonly TimeProvider _timeProvider;

    public UpdateAmenityCommandHandler(
        IValidator<UpdateAmenityCommand> validator,
        IAmenityRepository amenityRepository,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _amenityRepository = amenityRepository;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateAmenityResult> HandleAsync(
        UpdateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateAmenityResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var amenity = await _amenityRepository.GetByIdAsync(
            command.AmenityId,
            cancellationToken);

        if (amenity is null)
        {
            return new UpdateAmenityResult(
                false,
                [AmenityErrors.NotFound]);
        }

        var nameExists =
            await _amenityRepository.ExistsByNameExceptAsync(
                command.Name,
                command.AmenityId,
                cancellationToken);

        if (nameExists)
        {
            return new UpdateAmenityResult(
                false,
                [AmenityErrors.AlreadyExists]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        amenity.Update(
            command.Name,
            now);

        await _amenityRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateAmenityResult(
            true,
            []);
    }
}

public sealed record UpdateAmenityResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);