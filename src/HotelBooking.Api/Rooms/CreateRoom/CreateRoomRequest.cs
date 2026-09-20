using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Rooms.CreateRoom;

public sealed record CreateRoomRequest(
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);