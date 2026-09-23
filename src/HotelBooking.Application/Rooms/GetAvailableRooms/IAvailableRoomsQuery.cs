namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public interface IAvailableRoomsQuery
{
    Task<IReadOnlyCollection<AvailableRoom>> GetAsync(
        GetAvailableRoomsQuery query,
        DateTime now,
        CancellationToken cancellationToken);
}