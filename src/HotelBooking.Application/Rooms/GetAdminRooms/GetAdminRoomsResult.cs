using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetAdminRooms;

public sealed record GetAdminRoomsResult(
    bool Succeeded,
    IReadOnlyCollection<AdminRoom> Rooms,
    IReadOnlyCollection<ApplicationError> Errors);