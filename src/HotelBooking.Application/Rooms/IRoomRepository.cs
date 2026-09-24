using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Room>> GetByIdsAsync(
    IReadOnlyCollection<int> roomIds,
    CancellationToken cancellationToken);

    // For Create Room
    Task<bool> ExistsByRoomNumberAsync(
        int hotelId,
        string roomNumber,
        CancellationToken cancellationToken);

    //For Update Room
    Task<bool> ExistsByRoomNumberExceptAsync(
    int hotelId,
    string roomNumber,
    int excludedRoomId,
    CancellationToken cancellationToken);

    void Add(Room room);

    Task<int> SaveChangesAsync(
    CancellationToken cancellationToken);
}
