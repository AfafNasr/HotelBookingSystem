using FluentValidation;
using HotelBooking.Application.Amenities;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
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
            return new AddHotelAmenityResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new AddHotelAmenityResult(
                false,
                [HotelErrors.NotFound]);

        }

        if (!HotelAccessPolicy.CanManage(
     hotel,
     _currentUserService))
        {
            return new AddHotelAmenityResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var amenityExists = await _amenityRepository.ExistsByIdAsync(
            command.AmenityId,
            cancellationToken);

        if (!amenityExists)
        {
            return new AddHotelAmenityResult(
                false,
               [AmenityErrors.NotFound]);
        }

        var alreadyAssigned = await _hotelAmenityRepository.ExistsAsync(
            command.HotelId,
            command.AmenityId,
            cancellationToken);

        if (alreadyAssigned)
        {
            return new AddHotelAmenityResult(
                false,
               [HotelAmenityErrors.AlreadyExists]);
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