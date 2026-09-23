using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Rooms.GetAvailableRooms;

public sealed record GetAvailableRoomsRequest(
    RoomType RoomType,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children);