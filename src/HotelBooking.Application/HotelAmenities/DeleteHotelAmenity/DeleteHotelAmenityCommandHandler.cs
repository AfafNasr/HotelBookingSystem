using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;

public sealed class DeleteHotelAmenityCommandHandler
{
    private readonly IValidator<DeleteHotelAmenityCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelAmenityRepository _hotelAmenityRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteHotelAmenityCommandHandler(
        IValidator<DeleteHotelAmenityCommand> validator,
        IHotelRepository hotelRepository,
        IHotelAmenityRepository hotelAmenityRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _hotelAmenityRepository = hotelAmenityRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteHotelAmenityResult> HandleAsync(
        DeleteHotelAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteHotelAmenityResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new DeleteHotelAmenityResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
            hotel,
            _currentUserService))
        {
            return new DeleteHotelAmenityResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var hotelAmenity =
            await _hotelAmenityRepository.GetAsync(
                command.HotelId,
                command.AmenityId,
                cancellationToken);

        if (hotelAmenity is null)
        {
            return new DeleteHotelAmenityResult(
                false,
                [HotelAmenityErrors.NotFound]);
        }

        _hotelAmenityRepository.Remove(hotelAmenity);

        await _hotelAmenityRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteHotelAmenityResult(
            true,
            []);
    }
}