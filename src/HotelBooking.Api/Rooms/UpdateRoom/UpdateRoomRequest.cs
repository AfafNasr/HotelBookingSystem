using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Rooms.UpdateRoom;

public sealed record UpdateRoomRequest(
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);