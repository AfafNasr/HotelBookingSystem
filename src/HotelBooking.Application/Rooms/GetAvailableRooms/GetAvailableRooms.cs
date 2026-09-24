using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed record GetAvailableRoomsQuery(
    int HotelId,
    RoomType RoomType,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children);