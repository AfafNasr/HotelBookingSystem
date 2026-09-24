using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.UpdateRoom;

public sealed record UpdateRoomResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);