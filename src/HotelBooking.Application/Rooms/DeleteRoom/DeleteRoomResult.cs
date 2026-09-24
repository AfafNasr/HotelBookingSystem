using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.DeleteRoom;

public sealed record DeleteRoomResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);