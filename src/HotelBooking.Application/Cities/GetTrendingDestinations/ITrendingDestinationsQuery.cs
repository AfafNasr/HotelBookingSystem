namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public interface ITrendingDestinationsQuery
{
    Task<IReadOnlyCollection<TrendingDestination>> GetAsync(
        DateTime from,
        int limit,
        CancellationToken cancellationToken);
}