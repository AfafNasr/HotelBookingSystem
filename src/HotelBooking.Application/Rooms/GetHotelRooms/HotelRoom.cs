using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.GetHotelRooms;

public sealed record HotelRoom(
    int Id,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);