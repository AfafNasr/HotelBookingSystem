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

    public UpdateRoomCommandHandler(
        IValidator<UpdateRoomCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateRoomResult> HandleAsync(
        UpdateRoomCommand command,
        CancellationToken cancellationToken)
    {
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
                new[]
                {
                    new ApplicationError(
                        "RoomNotFound",
                        "The specified room does not exist.",
                        ErrorType.NotFound)
                });
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            room.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new UpdateRoomResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelNotFound",
                        "The hotel associated with this room does not exist.",
                        ErrorType.NotFound)
                });
        }

        var isAdmin = _currentUserService.IsInRole(Roles.Admin);
        var isOwner = hotel.OwnerId == _currentUserService.UserId;

        if (!isAdmin && !isOwner)
        {
            return new UpdateRoomResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You are not allowed to update rooms for this hotel.",
                        ErrorType.Authorization)
                });
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
                new[]
                {
                    new ApplicationError(
                        "RoomNumberAlreadyExists",
                        "A room with this number already exists in the hotel.",
                        ErrorType.Conflict)
                });
        }

        room.Update(
            command.RoomNumber,
            command.RoomType,
            command.Description,
            command.AdultsCapacity,
            command.ChildrenCapacity,
            command.PricePerNight,
            DateTime.UtcNow);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateRoomResult(
            true,
            Array.Empty<ApplicationError>());
    }
}