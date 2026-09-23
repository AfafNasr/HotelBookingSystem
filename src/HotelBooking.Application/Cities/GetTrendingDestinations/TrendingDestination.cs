namespace HotelBooking.Application.Cities.GetTrendingDestinations;

public sealed record TrendingDestination(
    int CityId,
    string CityName,
    string? ThumbnailStorageKey);