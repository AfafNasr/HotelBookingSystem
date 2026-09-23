using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class RecentlyVisitedHotelRepository
    : IRecentlyVisitedHotelRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RecentlyVisitedHotelRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordVisitAsync(
        string userId,
        int hotelId,
        DateTime visitedAt,
        CancellationToken cancellationToken)
    {
        var existingVisit =
            await _dbContext.RecentlyVisitedHotels
                .SingleOrDefaultAsync(
                    visit =>
                        visit.UserId == userId &&
                        visit.HotelId == hotelId,
                    cancellationToken);

        if (existingVisit is null)
        {
            var visit = new RecentlyVisitedHotel(
                userId,
                hotelId,
                visitedAt);

            _dbContext.RecentlyVisitedHotels.Add(visit);
        }
        else
        {
            existingVisit.MarkVisited(visitedAt);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}