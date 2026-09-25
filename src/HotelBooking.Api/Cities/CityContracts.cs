namespace HotelBooking.Api.Cities;

public sealed record CreateCityRequest(
    string Name,
    string CountryCode,
    string? PostOffice);

public sealed record CreateCityResponse(
    int Id);

public sealed record UpdateCityRequest(
    string Name,
    string CountryCode,
    string? PostOffice);

public sealed record TrendingDestinationResponse(
    int CityId,
    string CityName,
    string? ThumbnailStorageKey);