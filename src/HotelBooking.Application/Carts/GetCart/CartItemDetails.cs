using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Carts.GetCart;

public sealed record CartItemDetails(
    int RoomId,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    bool IsRoomActive,
    DateTime AddedAt);