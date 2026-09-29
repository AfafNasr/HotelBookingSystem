namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public interface ITrendingDestinationsQuery
{
    Task<IReadOnlyCollection<TrendingDestination>> GetAsync(
        DateTime from,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record TrendingDestination(
    int CityId,
    string CityName,
    string? ThumbnailStorageKey);