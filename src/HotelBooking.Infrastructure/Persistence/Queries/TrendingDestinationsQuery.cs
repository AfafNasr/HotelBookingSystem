using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class TrendingDestinationsQuery : ITrendingDestinationsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public TrendingDestinationsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TrendingDestination>> GetAsync(
        DateTime from,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Status == BookingStatus.Confirmed &&
                booking.CreatedAt >= from &&
                !booking.Hotel.IsDeleted &&
                !booking.Hotel.City.IsDeleted)
            .GroupBy(booking => new
            {
                booking.Hotel.CityId,
                booking.Hotel.City.Name,
                booking.Hotel.City.ThumbnailStorageKey
            })
            .Select(group => new
            {
                group.Key.CityId,
                CityName = group.Key.Name,
                group.Key.ThumbnailStorageKey,
                BookingCount = group.Count()
            })
            .OrderByDescending(destination => destination.BookingCount)
            .ThenBy(destination => destination.CityId)
            .Take(limit)
            .Select(destination => new TrendingDestination(
                destination.CityId,
                destination.CityName,
                destination.ThumbnailStorageKey))
            .ToListAsync(cancellationToken);
    }
}