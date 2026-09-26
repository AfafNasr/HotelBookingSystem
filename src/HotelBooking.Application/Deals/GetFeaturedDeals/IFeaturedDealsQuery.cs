namespace HotelBooking.Application.Deals.GetFeaturedDeals;

public interface IFeaturedDealsQuery
{
    Task<IReadOnlyCollection<FeaturedDealItem>> GetAsync(
        DateOnly today,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record FeaturedDealItem(
    int HotelId,
    string HotelName,
    string CityName,
    string? Address,
    int StarRating,
    decimal DiscountPercentage,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight,
    string? ThumbnailStorageKey);