using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Rooms;

public sealed record SaveRoomRequest(
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);

public sealed record CreateRoomResponse(
    int Id);