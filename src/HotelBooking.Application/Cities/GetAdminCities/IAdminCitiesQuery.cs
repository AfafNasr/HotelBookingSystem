namespace HotelBooking.Application.Cities.GetAdminCities;

public interface IAdminCitiesQuery
{
    Task<IReadOnlyCollection<AdminCity>> GetAsync(
        GetAdminCitiesQuery query,
        CancellationToken cancellationToken);
}