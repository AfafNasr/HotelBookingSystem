namespace HotelBooking.Application.Cities.UpdateCity;

public sealed record UpdateCityCommand(
    int CityId,
    string Name,
    string CountryCode,
    string? PostOffice);