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
        return await _dbContext.Deals
            .AsNoTracking()
            .Where(deal =>
                deal.StartDate <= today &&
                deal.EndDate >= today &&
                !deal.Hotel.IsDeleted &&
                _dbContext.Room.Any(room =>
                    room.HotelId == deal.HotelId &&
                    !room.IsDeleted))
            .OrderByDescending(deal => deal.CreatedAt)
            .ThenByDescending(deal => deal.DiscountPercentage)
            .ThenByDescending(deal => deal.Id)
            .Take(limit)
            .Select(deal => new FeaturedDealItem(
                deal.HotelId,
                deal.Hotel.Name,
                deal.Hotel.City.Name,
                deal.Hotel.Address,
                deal.Hotel.StarRating,
                deal.DiscountPercentage,

                _dbContext.Room
                    .Where(room =>
                        room.HotelId == deal.HotelId &&
                        !room.IsDeleted)
                    .Min(room => room.PricePerNight),

                _dbContext.Room
                    .Where(room =>
                        room.HotelId == deal.HotelId &&
                        !room.IsDeleted)
                    .Min(room => room.PricePerNight)
                    * (1 - deal.DiscountPercentage / 100m),

                _dbContext.HotelImages
                    .Where(image => image.HotelId == deal.HotelId)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .Select(image => image.StorageKey)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }
}