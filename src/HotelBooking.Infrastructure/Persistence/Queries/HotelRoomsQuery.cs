using HotelBooking.Application.Rooms.GetHotelRooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class HotelRoomsQuery : IHotelRoomsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public HotelRoomsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<HotelRoom>> GetAsync(
        int hotelId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Room
            .AsNoTracking()
            .Where(room =>
                room.HotelId == hotelId &&
                !room.IsDeleted)
            .OrderBy(room => room.RoomNumber)
            .Select(room => new HotelRoom(
                room.Id,
                room.RoomNumber,
                room.RoomType,
                room.Description,
                room.AdultsCapacity,
                room.ChildrenCapacity,
                room.PricePerNight))
            .ToListAsync(cancellationToken);
    }
}