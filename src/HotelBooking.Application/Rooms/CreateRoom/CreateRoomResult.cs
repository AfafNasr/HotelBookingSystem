using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.CreateRoom;

public sealed record CreateRoomResult(
    bool Succeeded,
    int? RoomId,
    IReadOnlyCollection<ApplicationError> Errors);