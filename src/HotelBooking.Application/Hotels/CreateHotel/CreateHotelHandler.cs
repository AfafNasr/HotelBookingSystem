using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.CreateHotel;

public sealed class CreateHotelCommandHandler
{
    private readonly IValidator<CreateHotelCommand> _validator;
    private readonly ICityRepository _cityRepository;
    private readonly IIdentityService _identityService;
    private readonly IHotelRepository _hotelRepository;

    public CreateHotelCommandHandler(
        IValidator<CreateHotelCommand> validator,
        ICityRepository cityRepository,
        IIdentityService identityService,
        IHotelRepository hotelRepository)
    {
        _validator = validator;
        _cityRepository = cityRepository;
        _identityService = identityService;
        _hotelRepository = hotelRepository;
    }

    public async Task<CreateHotelResult> HandleAsync(
        CreateHotelCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CreateHotelResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var city = await _cityRepository.GetByIdAsync(
            command.CityId,
            cancellationToken);

        if (city is null)
        {
            return new CreateHotelResult(
                false,
                null,
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
            return new CreateHotelResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "InvalidHotelOwner",
                        "The specified user must be a hotel owner.",
                        ErrorType.Validation)
                });
        }

        var hotel = new Hotel(
            command.Name,
            command.CityId,
            command.OwnerId,
            command.StarRating,
            command.Category,
            DateTime.UtcNow);

        _hotelRepository.Add(hotel);

        await _hotelRepository.SaveChangesAsync(cancellationToken);

        return new CreateHotelResult(
            true,
            hotel.Id,
            Array.Empty<ApplicationError>());
    }
}