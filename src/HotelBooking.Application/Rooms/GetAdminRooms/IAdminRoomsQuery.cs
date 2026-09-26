namespace HotelBooking.Application.Rooms.GetAdminRooms;

public interface IAdminRoomsQuery
{
    Task<IReadOnlyCollection<AdminRoom>> GetAsync(
        GetAdminRoomsQuery query,
        DateTime now,
        CancellationToken cancellationToken);
}
public sealed record AdminRoom(
    int Id,
    string RoomNumber,
    bool IsAvailable,
    int AdultsCapacity,
    int ChildrenCapacity,
    DateTime CreatedAt,
    DateTime? UpdatedAt);