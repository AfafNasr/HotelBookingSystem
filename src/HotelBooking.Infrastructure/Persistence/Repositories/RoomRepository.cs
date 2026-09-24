using HotelBooking.Application.Rooms;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoomRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Room?> GetByIdAsync(
    int roomId,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Room
            .FirstOrDefaultAsync(
                room => room.Id == roomId && !room.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Room>> GetByIdsAsync(
    IReadOnlyCollection<int> roomIds,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Room
            .Where(room =>
                roomIds.Contains(room.Id) &&
                !room.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByRoomNumberAsync(
        int hotelId,
        string roomNumber,
        CancellationToken cancellationToken)
    {
        var normalizedRoomNumber = roomNumber.Trim();

        return _dbContext.Room.AnyAsync(
            room =>
                room.HotelId == hotelId &&
                room.RoomNumber == normalizedRoomNumber,
            cancellationToken);
    }

    public Task<bool> ExistsByRoomNumberExceptAsync(
    int hotelId,
    string roomNumber,
    int excludedRoomId,
    CancellationToken cancellationToken)
    {
        var normalizedRoomNumber = roomNumber.Trim();

        return _dbContext.Room.AnyAsync(
            room =>
                room.HotelId == hotelId &&
                room.Id != excludedRoomId &&
                room.RoomNumber == normalizedRoomNumber,
            cancellationToken);
    }

    public void Add(Room room)
    {
        _dbContext.Room.Add(room);
    }

    public Task<int> SaveChangesAsync(
     CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
