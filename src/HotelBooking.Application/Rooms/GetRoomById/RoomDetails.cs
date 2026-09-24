using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.GetRoomById;

public sealed record RoomDetails(
    int Id,
    int HotelId,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);