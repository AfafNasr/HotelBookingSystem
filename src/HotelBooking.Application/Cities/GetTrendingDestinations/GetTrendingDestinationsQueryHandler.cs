namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public sealed class GetTrendingDestinationsQueryHandler
{
    private const int TrendingPeriodDays = 30;
    private const int TrendingDestinationsLimit = 5;

    private readonly ITrendingDestinationsQuery _trendingDestinationsQuery;

    public GetTrendingDestinationsQueryHandler(
        ITrendingDestinationsQuery trendingDestinationsQuery)
    {
        _trendingDestinationsQuery = trendingDestinationsQuery;
    }

    public async Task<GetTrendingDestinationsResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.AddDays(-TrendingPeriodDays);

        var destinations = await _trendingDestinationsQuery.GetAsync(
            from,
            TrendingDestinationsLimit,
            cancellationToken);

        return new GetTrendingDestinationsResult(
            true,
            destinations,
            []);
    }
}