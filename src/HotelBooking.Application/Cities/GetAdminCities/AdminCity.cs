namespace HotelBooking.Application.Cities.GetAdminCities;

public sealed record AdminCity(
    int Id,
    string Name,
    string Country,
    string? PostOffice,
    int NumberOfHotels,
    DateTime CreatedAt,
    DateTime? UpdatedAt);