using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetRoomById;

public sealed record GetRoomByIdResult(
    bool Succeeded,
    RoomDetails? Room,
    IReadOnlyCollection<ApplicationError> Errors);