namespace HotelBooking.Application.Rooms.GetHotelRooms;

public interface IHotelRoomsQuery
{
    Task<IReadOnlyCollection<HotelRoom>> GetAsync(
        int hotelId,
        CancellationToken cancellationToken);
}