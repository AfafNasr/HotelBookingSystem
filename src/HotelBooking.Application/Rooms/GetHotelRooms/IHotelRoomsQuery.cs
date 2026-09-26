using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms.GetHotelRooms;

public interface IHotelRoomsQuery
{
    Task<IReadOnlyCollection<HotelRoom>> GetAsync(
        int hotelId,
        CancellationToken cancellationToken);
}

public sealed record HotelRoom(
    int Id,
    string RoomNumber,
    RoomType RoomType,
    string? Description,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight);