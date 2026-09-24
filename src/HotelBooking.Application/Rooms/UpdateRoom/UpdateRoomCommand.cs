using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.UpdateRoom;

public sealed record UpdateRoomCommand(
    int RoomId,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);