namespace HotelBooking.Application.Cities.GetAdminCities;

public interface IAdminCitiesQuery
{
    Task<IReadOnlyCollection<AdminCity>> GetAsync(
        GetAdminCitiesQuery query,
        CancellationToken cancellationToken);
}

public sealed record AdminCity(
    int Id,
    string Name,
    string Country,
    string? PostOffice,
    int NumberOfHotels,
    DateTime CreatedAt,
    DateTime? UpdatedAt);