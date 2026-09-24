using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed record AvailableRoom(
    int Id,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    IReadOnlyCollection<AvailableRoomImage> Images);

public sealed record AvailableRoomImage(
    string StorageKey,
    int DisplayOrder,
    bool IsPrimary);