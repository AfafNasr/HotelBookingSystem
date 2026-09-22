using HotelBooking.Application.Deals;
using HotelBooking.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class DealRepository : IDealRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DealRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasOverlappingDealAsync(
        int hotelId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Deals.AnyAsync(
            deal =>
                deal.HotelId == hotelId &&
                deal.StartDate <= endDate &&
                deal.EndDate >= startDate,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Deal>> GetOverlappingDealsAsync(
     int hotelId,
     DateOnly checkInDate,
     DateOnly checkOutDate,
     CancellationToken cancellationToken)
    {
        return await _dbContext.Deals
            .AsNoTracking()
            .Where(deal =>
                deal.HotelId == hotelId &&
                deal.StartDate < checkOutDate &&
                deal.EndDate >= checkInDate)
            .OrderBy(deal => deal.StartDate)
            .ToListAsync(cancellationToken);
    }

    public void Add(Deal deal)
    {
        _dbContext.Deals.Add(deal);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}