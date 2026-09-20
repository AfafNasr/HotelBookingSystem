using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Domain.Amenities;

namespace HotelBooking.Application.Amenities.CreateAmenity;

public sealed class CreateAmenityCommandHandler
{
    private readonly IValidator<CreateAmenityCommand> _validator;
    private readonly IAmenityRepository _amenityRepository;

    public CreateAmenityCommandHandler(
        IValidator<CreateAmenityCommand> validator,
        IAmenityRepository amenityRepository)
    {
        _validator = validator;
        _amenityRepository = amenityRepository;
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
            var errors = validationResult.Errors
                .Select(error => new ApplicationError(
                    error.PropertyName,
                    error.ErrorMessage,
                    ErrorType.Validation))
                .ToArray();

            return new CreateAmenityResult(
                false,
                null,
                errors);
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

        var amenity = new Amenity(
            command.Name,
            DateTime.UtcNow);

        _amenityRepository.Add(amenity);

        await _amenityRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateAmenityResult(
            true,
            amenity.Id,
            Array.Empty<ApplicationError>());
    }
}