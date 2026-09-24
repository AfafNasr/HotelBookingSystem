using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed record GetAvailableRoomsResult(
    bool Succeeded,
    IReadOnlyCollection<AvailableRoom> Rooms,
    IReadOnlyCollection<ApplicationError> Errors);