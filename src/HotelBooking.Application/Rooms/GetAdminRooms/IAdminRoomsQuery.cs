namespace HotelBooking.Application.Rooms.GetAdminRooms;

public interface IAdminRoomsQuery
{
    Task<IReadOnlyCollection<AdminRoom>> GetAsync(
        GetAdminRoomsQuery query,
        DateTime now,
        CancellationToken cancellationToken);
}