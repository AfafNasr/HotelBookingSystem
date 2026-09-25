using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed class CompleteHotelProfileCommandHandler
{
    private readonly IValidator<CompleteHotelProfileCommand> _validator;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public CompleteHotelProfileCommandHandler(
        IValidator<CompleteHotelProfileCommand> validator,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CompleteHotelProfileResult> HandleAsync(
        CompleteHotelProfileCommand command,
        CancellationToken cancellationToken)
    {

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CompleteHotelProfileResult
                (false,
                validationResult.ToApplicationErrors());
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            command.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new CompleteHotelProfileResult(
                false,
               [HotelErrors.NotFound]);
        }

        var currentUserId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(currentUserId) ||
             !HotelAccessPolicy.CanManage(
             hotel,
            _currentUserService))
        {
            return new CompleteHotelProfileResult(
                false,
              [HotelErrors.ManagementForbidden]);
        }

        hotel.CompleteProfile(
            command.Description,
            command.Address,
            command.Latitude,
            command.Longitude,
           now);

        await _hotelRepository.SaveChangesAsync(cancellationToken);

        return new CompleteHotelProfileResult(
            true,
            Array.Empty<ApplicationError>());
    }
}