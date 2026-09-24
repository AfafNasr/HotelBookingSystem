using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;

namespace HotelBooking.Application.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandHandler
{
    private readonly IValidator<DeleteRoomCommand> _validator;
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteRoomCommandHandler(
        IValidator<DeleteRoomCommand> validator,
        IRoomRepository roomRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUserService)
    {
        _validator = validator;
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteRoomResult> HandleAsync(
        DeleteRoomCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeleteRoomResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var room = await _roomRepository.GetByIdAsync(
            command.RoomId,
            cancellationToken);

        if (room is null)
        {
            return new DeleteRoomResult(
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
            return new DeleteRoomResult(
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
            return new DeleteRoomResult(
                false,
                new[]
                {
                    new ApplicationError(
                        "HotelOwnershipRequired",
                        "You are not allowed to delete rooms for this hotel.",
                        ErrorType.Authorization)
                });
        }

        room.Delete(DateTime.UtcNow);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteRoomResult(
            true,
            Array.Empty<ApplicationError>());
    }
}