using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetRoomById;

public sealed class GetRoomByIdQueryHandler
{
    private readonly IRoomRepository _roomRepository;

    public GetRoomByIdQueryHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<GetRoomByIdResult> HandleAsync(
        GetRoomByIdQuery query,
        CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(
            query.RoomId,
            cancellationToken);
        if (room is null)
        {
            return new GetRoomByIdResult(
                false,
                null,
                [RoomErrors.NotFound]);
        }

        var roomDetails = new RoomDetails(
            room.Id,
            room.HotelId,
            room.RoomNumber,
            room.RoomType,
            room.Description,
            room.AdultsCapacity,
            room.ChildrenCapacity,
            room.PricePerNight);

        return new GetRoomByIdResult(
            true,
            roomDetails,
            Array.Empty<ApplicationError>());
    }
}

public sealed record GetRoomByIdResult(
    bool Succeeded,
    RoomDetails? Room,
    IReadOnlyCollection<ApplicationError> Errors);