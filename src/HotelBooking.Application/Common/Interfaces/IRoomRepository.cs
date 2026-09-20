using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Common.Interfaces;

public interface IRoomRepository
{
    Task<bool> ExistsByRoomNumberAsync(
        int hotelId,
        string roomNumber,
        CancellationToken cancellationToken);

    void Add(Room room);

    Task<int> SaveChangesAsync(
    CancellationToken cancellationToken);
}
