using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.GetHotelRooms;

public sealed record GetHotelRoomsResult(
    bool Succeeded,
    IReadOnlyCollection<HotelRoom> Rooms,
    IReadOnlyCollection<ApplicationError> Errors);