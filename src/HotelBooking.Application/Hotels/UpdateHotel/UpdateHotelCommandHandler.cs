using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed class UpdateHotelCommandHandler
{
    private readonly IValidator<UpdateHotelCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;
    private readonly IIdentityService _identityService;
    private readonly TimeProvider _timeProvider;


    public UpdateHotelCommandHandler(
        IValidator<UpdateHotelCommand> validator,
        IHotelRepository hotelRepository,
        ICityRepository cityRepository,
        IIdentityService identityService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _cityRepository = cityRepository;
        _identityService = identityService;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateHotelResult> HandleAsync(
        UpdateHotelCommand command,
        CancellationToken cancellationToken)
    {

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateHotelResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UpdateHotelResult(
                false,
              [HotelErrors.NotFound]);

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
            now);

        await _hotelRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateHotelResult(
            true,
            Array.Empty<ApplicationError>());
    }
}