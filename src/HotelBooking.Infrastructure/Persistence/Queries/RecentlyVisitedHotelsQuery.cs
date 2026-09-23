using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class RecentlyVisitedHotelsQuery
    : IRecentlyVisitedHotelsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public RecentlyVisitedHotelsQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RecentlyVisitedHotelItem>> GetAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.RecentlyVisitedHotels
            .AsNoTracking()
            .Where(visit =>
                visit.UserId == userId &&
                !visit.Hotel.IsDeleted)
            .OrderByDescending(visit => visit.LastVisitedAt)
            .Take(limit)
            .Select(visit => new RecentlyVisitedHotelItem(
                visit.HotelId,
                visit.Hotel.Name,
                visit.Hotel.City.Name,
                visit.Hotel.StarRating,

                _dbContext.Room
                    .Where(room =>
                        room.HotelId == visit.HotelId &&
                        !room.IsDeleted)
                    .Select(room => (decimal?)room.PricePerNight)
                    .Min(),

                _dbContext.HotelImages
                    .Where(image =>
                        image.HotelId == visit.HotelId)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => image.StorageKey)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}