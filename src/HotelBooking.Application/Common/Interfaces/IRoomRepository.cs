using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Common.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Room>> GetByIdsAsync(
    IReadOnlyCollection<int> roomIds,
    CancellationToken cancellationToken);

    Task<bool> ExistsByRoomNumberAsync(
        int hotelId,
        string roomNumber,
        CancellationToken cancellationToken);

    void Add(Room room);

    Task<int> SaveChangesAsync(
    CancellationToken cancellationToken);
}
