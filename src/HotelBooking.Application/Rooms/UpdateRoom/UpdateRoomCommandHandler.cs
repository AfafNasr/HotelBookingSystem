using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandHandler
{
    private readonly IValidator<UpdateRoomCommand> _validator;
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateRoomCommandHandler(
        IValidator<UpdateRoomCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateRoomResult> HandleAsync(
        UpdateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateRoomResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var room = await _roomRepository.GetByIdAsync(
            command.RoomId,
            cancellationToken);

        if (room is null)
        {
            return new UpdateRoomResult(
                false,
                [RoomErrors.NotFound]);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            room.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UpdateRoomResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
        hotel,
        _currentUserService))
        {
            return new UpdateRoomResult(
                false,
              [HotelErrors.ManagementForbidden]);
        }

        var roomNumberExists =
            await _roomRepository.ExistsByRoomNumberExceptAsync(
                room.HotelId,
                command.RoomNumber,
                room.Id,
                cancellationToken);

        if (roomNumberExists)
        {
            return new UpdateRoomResult(
                false,
                [RoomErrors.NumberAlreadyExists]);
        }

        room.Update(
            command.RoomNumber,
            command.RoomType,
            command.Description,
            command.AdultsCapacity,
            command.ChildrenCapacity,
            command.PricePerNight,
           now);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateRoomResult(
            true,
            Array.Empty<ApplicationError>());
    }
}