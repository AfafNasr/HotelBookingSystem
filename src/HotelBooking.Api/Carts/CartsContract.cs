using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Carts;


public sealed record AddToCartRequest(
    int RoomId);

public sealed record AddToCartResponse(
    int CartId);

public sealed record GetCartResponse(
    int? CartId,
    int? HotelId,
    string? HotelName,
    IReadOnlyCollection<CartItemResponse> Items);

public sealed record CartItemResponse(
    int RoomId,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    bool IsRoomActive,
    DateTime AddedAt);
