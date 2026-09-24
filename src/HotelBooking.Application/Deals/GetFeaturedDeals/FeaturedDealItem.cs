namespace HotelBooking.Application.Deals.GetFeaturedDeals;

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