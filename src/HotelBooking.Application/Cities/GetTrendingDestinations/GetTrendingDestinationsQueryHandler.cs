using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public sealed class GetTrendingDestinationsQueryHandler
{
    private const int TrendingPeriodDays = 30;
    private const int TrendingDestinationsLimit = 5;

    private readonly ITrendingDestinationsQuery _trendingDestinationsQuery;
    private readonly TimeProvider _timeProvider;

    public GetTrendingDestinationsQueryHandler(
        ITrendingDestinationsQuery trendingDestinationsQuery,
        TimeProvider timeProvider )
    {
        _trendingDestinationsQuery = trendingDestinationsQuery;
        _timeProvider = timeProvider;
    }

    public async Task<GetTrendingDestinationsResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var from = now.AddDays(-TrendingPeriodDays);

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

public sealed record GetTrendingDestinationsResult(
    bool Succeeded,
    IReadOnlyCollection<TrendingDestination> Destinations,
    IReadOnlyCollection<ApplicationError> Errors);