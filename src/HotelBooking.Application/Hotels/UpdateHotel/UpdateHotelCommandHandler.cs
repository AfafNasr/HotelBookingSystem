using FluentValidation;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed class UpdateHotelCommandHandler
{
    private readonly IValidator<UpdateHotelCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;
    private readonly IIdentityService _identityService;

    public UpdateHotelCommandHandler(
        IValidator<UpdateHotelCommand> validator,
        IHotelRepository hotelRepository,
        ICityRepository cityRepository,
        IIdentityService identityService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _cityRepository = cityRepository;
        _identityService = identityService;
    }

    public async Task<UpdateHotelResult> HandleAsync(
        UpdateHotelCommand command,
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

            return new UpdateHotelResult(
                false,
                errors);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UpdateHotelResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The specified hotel does not exist.",
                        ErrorType.NotFound)
                });
        }

        var city = await _cityRepository.GetByIdAsync(
            command.CityId,
            cancellationToken);

        if (city is null)
        {
            return new UpdateHotelResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "CityNotFound",
                        "The specified city does not exist.",
                        ErrorType.Validation)
                });
        }

        var isHotelOwner = await _identityService.IsUserInRoleAsync(
            command.OwnerId,
            Roles.HotelOwner);

        if (!isHotelOwner)
        {
            return new UpdateHotelResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "InvalidHotelOwner",
                        "The specified user must be a hotel owner.",
                        ErrorType.Validation)
                });
        }

        hotel.Update(
            command.Name,
            command.CityId,
            command.OwnerId,
            command.StarRating,
            command.Category,
            command.Description,
            command.Address,
            command.Latitude,
            command.Longitude,
            DateTime.UtcNow);

        await _hotelRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateHotelResult(
            true,
            Array.Empty<ApplicationError>());
    }
}