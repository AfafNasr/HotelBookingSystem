using HotelBooking.Application.Deals.GetFeaturedDeals;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class FeaturedDealsQuery : IFeaturedDealsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public FeaturedDealsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<FeaturedDealItem>> GetAsync(
        DateOnly today,
        int limit,
        CancellationToken cancellationToken)
    {
        var roomPriceQuery = _dbContext.Room
            .AsNoTracking()
            .Where(room => !room.IsDeleted)
            .GroupBy(room => room.HotelId)
            .Select(group => new
            {
                HotelId = group.Key,
                MinPrice = group.Min(
                    room => room.PricePerNight)
            });

        var query =
            from deal in _dbContext.Deals.AsNoTracking()
            join roomPrice in roomPriceQuery
                on deal.HotelId equals roomPrice.HotelId
            where
                deal.StartDate <= today &&
                deal.EndDate >= today &&
                !deal.Hotel.IsDeleted
            orderby
                deal.CreatedAt descending,
                deal.DiscountPercentage descending,
                deal.Id descending
            select new FeaturedDealItem(
                deal.HotelId,
                deal.Hotel.Name,
                deal.Hotel.City.Name,
                deal.Hotel.Address,
                deal.Hotel.StarRating,
                deal.DiscountPercentage,
                roomPrice.MinPrice,
                roomPrice.MinPrice *
                    (1 - deal.DiscountPercentage / 100m),
                _dbContext.HotelImages
                    .Where(image =>
                        image.HotelId == deal.HotelId)
                    .OrderByDescending(image =>
                        image.IsPrimary)
                    .ThenBy(image =>
                        image.DisplayOrder)
                    .Select(image =>
                        image.StorageKey)
                    .FirstOrDefault());

        return await query
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}