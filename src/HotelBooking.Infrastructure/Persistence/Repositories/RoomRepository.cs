using HotelBooking.Application.Common.Interfaces;
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
