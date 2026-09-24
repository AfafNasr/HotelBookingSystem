namespace HotelBooking.Application.Rooms.GetAdminRooms;

public sealed record AdminRoom(
    int Id,
    string RoomNumber,
    bool IsAvailable,
    int AdultsCapacity,
    int ChildrenCapacity,
    DateTime CreatedAt,
    DateTime? UpdatedAt);