namespace HotelBooking.Api.Deals.GetFeaturedDeals;

public sealed record GetFeaturedDealsResponse(
    IReadOnlyCollection<FeaturedDealResponse> Deals);

public sealed record FeaturedDealResponse(
    int HotelId,
    string HotelName,
    string CityName,
    string? Address,
    int StarRating,
    decimal DiscountPercentage,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight,
    string? ThumbnailStorageKey);
