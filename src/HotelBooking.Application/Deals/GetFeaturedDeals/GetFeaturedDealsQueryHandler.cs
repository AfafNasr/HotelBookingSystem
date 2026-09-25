namespace HotelBooking.Application.Deals.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandler
{
    private const int FeaturedDealsLimit = 5;

    private readonly IFeaturedDealsQuery _featuredDealsQuery;
    private readonly TimeProvider _timeProvider;

    public GetFeaturedDealsQueryHandler(
        IFeaturedDealsQuery featuredDealsQuery,
        TimeProvider timeProvider)
    {
        _featuredDealsQuery = featuredDealsQuery;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<FeaturedDealItem>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(
     _timeProvider.GetUtcNow().UtcDateTime);

        return await _featuredDealsQuery.GetAsync(
            today,
            FeaturedDealsLimit,
            cancellationToken);
    }
}