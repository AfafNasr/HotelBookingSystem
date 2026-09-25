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
    private readonly TimeProvider _timeProvider;

    public DeleteRoomCommandHandler(
        IValidator<DeleteRoomCommand> validator,
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
                [RoomErrors.NotFound]);
        }

        var hotel = await _hotelRepository.GetByIdAsync(
            room.HotelId,
            cancellationToken);

        if (hotel is null)
        {
            return new DeleteRoomResult(
                false,
                [HotelErrors.NotFound]);
        }

        if (!HotelAccessPolicy.CanManage(
        hotel,
        _currentUserService))
        {
            return new DeleteRoomResult(
                false,
                [HotelErrors.ManagementForbidden]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var hasActiveOrUpcomingBookings =
            await _roomRepository.HasActiveOrUpcomingBookingsAsync(
                room.Id,
                today,
                now,
                cancellationToken);

        if (hasActiveOrUpcomingBookings)
        {
            return new DeleteRoomResult(
                false,
                [RoomErrors.HasActiveBookings]);
        }

        room.Delete(now);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteRoomResult(
            true,
            Array.Empty<ApplicationError>());
    }
}