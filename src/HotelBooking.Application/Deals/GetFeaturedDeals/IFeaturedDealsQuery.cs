namespace HotelBooking.Application.Deals.GetFeaturedDeals;

public interface IFeaturedDealsQuery
{
    Task<IReadOnlyCollection<FeaturedDealItem>> GetAsync(
        DateOnly today,
        int limit,
        CancellationToken cancellationToken);
}