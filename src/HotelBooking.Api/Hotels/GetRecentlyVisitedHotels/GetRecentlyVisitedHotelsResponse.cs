namespace HotelBooking.Api.Hotels.GetRecentlyVisitedHotels;

public sealed record GetRecentlyVisitedHotelsResponse(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    decimal? StartingPricePerNight,
    string? ThumbnailStorageKey);