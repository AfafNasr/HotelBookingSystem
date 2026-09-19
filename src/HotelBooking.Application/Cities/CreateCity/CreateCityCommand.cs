namespace HotelBooking.Application.Cities.CreateCity;

public sealed record CreateCityCommand(
    string Name,
    string CountryCode,
    string? PostOffice);