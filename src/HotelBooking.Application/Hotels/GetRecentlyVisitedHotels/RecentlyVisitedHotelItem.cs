namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public sealed record RecentlyVisitedHotelItem(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    decimal? StartingPricePerNight,
    string? ThumbnailStorageKey);