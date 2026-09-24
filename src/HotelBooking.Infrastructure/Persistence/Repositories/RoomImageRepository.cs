using HotelBooking.Application.Rooms;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class RoomImageRepository : IRoomImageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoomImageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(RoomImage roomImage)
    {
        _dbContext.RoomImages.Add(roomImage);
    }

    public async Task<int> GetNextDisplayOrderAsync(
        int roomId,
        CancellationToken cancellationToken)
    {
        var maxDisplayOrder = await _dbContext.RoomImages
            .Where(image => image.RoomId == roomId)
            .Select(image => (int?)image.DisplayOrder)
            .MaxAsync(cancellationToken);

        return (maxDisplayOrder ?? -1) + 1;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}