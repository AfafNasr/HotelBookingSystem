namespace HotelBooking.Api.Cities.CreateCity;

public sealed record CreateCityRequest(
    string Name,
    string CountryCode,
    string? PostOffice);