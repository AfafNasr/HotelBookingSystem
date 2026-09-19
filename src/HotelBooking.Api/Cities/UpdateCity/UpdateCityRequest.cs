namespace HotelBooking.Api.Cities.UpdateCity;

public sealed record UpdateCityRequest(
    string Name,
    string CountryCode,
    string? PostOffice);