using FluentValidation;
using HotelBooking.Application.Amenities;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.HotelAmenities.AddHotelAmenity;

public sealed class AddHotelAmenityCommandHandler
{
    private readonly IValidator<AddHotelAmenityCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IAmenityRepository _amenityRepository;
    private readonly IHotelAmenityRepository _hotelAmenityRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddHotelAmenityCommandHandler(
        IValidator<AddHotelAmenityCommand> validator,
        IHotelRepository hotelRepository,
        IAmenityRepository amenityRepository,
        IHotelAmenityRepository hotelAmenityRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _amenityRepository = amenityRepository;
        _hotelAmenityRepository = hotelAmenityRepository;
        _currentUserService = currentUserService;
    }

    public async Task<AddHotelAmenityResult> HandleAsync(
        AddHotelAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
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

            return new AddHotelAmenityResult(
                false,
                errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new AddHotelAmenityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The specified hotel does not exist.",
                        ErrorType.NotFound)
                });
        }

        if (hotel.OwnerId != _currentUserService.UserId)
        {
            return new AddHotelAmenityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You are not allowed to manage amenities for this hotel.",
                        ErrorType.Authorization)
                });
        }

        var amenityExists = await _amenityRepository.ExistsByIdAsync(
            command.AmenityId,
            cancellationToken);

        if (!amenityExists)
        {
            return new AddHotelAmenityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "AmenityNotFound",
                        "The specified amenity does not exist.",
                        ErrorType.NotFound)
                });
        }

        var alreadyAssigned = await _hotelAmenityRepository.ExistsAsync(
            command.HotelId,
            command.AmenityId,
            cancellationToken);

        if (alreadyAssigned)
        {
            return new AddHotelAmenityResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelAmenityAlreadyExists",
                        "The amenity is already assigned to this hotel.",
                        ErrorType.Conflict)
                });
        }

        var hotelAmenity = new HotelAmenity(
            command.HotelId,
            command.AmenityId);

        _hotelAmenityRepository.Add(hotelAmenity);

        await _hotelAmenityRepository.SaveChangesAsync(
            cancellationToken);

        return new AddHotelAmenityResult(
            true,
            Array.Empty<ApplicationError>());
    }
}