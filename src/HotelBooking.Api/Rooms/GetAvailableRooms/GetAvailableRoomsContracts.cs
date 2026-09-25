using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Rooms.GetAvailableRooms;

public sealed record GetAvailableRoomsRequest(
    RoomType RoomType,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children);
public sealed record GetAvailableRoomsResponse(
    int Id,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    IReadOnlyCollection<AvailableRoomImageResponse> Images);

public sealed record AvailableRoomImageResponse(
    string StorageKey,
    int DisplayOrder,
    bool IsPrimary);