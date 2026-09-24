namespace HotelBooking.Application.Deals.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandler
{
    private const int FeaturedDealsLimit = 5;

    private readonly IFeaturedDealsQuery _featuredDealsQuery;

    public GetFeaturedDealsQueryHandler(
        IFeaturedDealsQuery featuredDealsQuery)
    {
        _featuredDealsQuery = featuredDealsQuery;
    }

    public async Task<IReadOnlyCollection<FeaturedDealItem>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _featuredDealsQuery.GetAsync(
            today,
            FeaturedDealsLimit,
            cancellationToken);
    }
}