using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.CreateRoom;

public sealed record CreateRoomCommand(
    int HotelId,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);