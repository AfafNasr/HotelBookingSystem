namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public interface IRecentlyVisitedHotelsQuery
{
    Task<IReadOnlyCollection<RecentlyVisitedHotelItem>> GetAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record RecentlyVisitedHotelItem(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    decimal? StartingPricePerNight,
    string? ThumbnailStorageKey);