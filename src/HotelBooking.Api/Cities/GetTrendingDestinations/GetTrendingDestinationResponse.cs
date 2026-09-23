namespace HotelBooking.Api.Cities.GetTrendingDestinations;

public sealed record GetTrendingDestinationResponse(
    int CityId,
    string CityName,
    string? ThumbnailStorageKey);