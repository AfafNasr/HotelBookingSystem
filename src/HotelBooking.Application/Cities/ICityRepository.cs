using HotelBooking.Domain.Cities;

namespace HotelBooking.Application.Cities;

public interface ICityRepository
{
    Task<City?> GetByIdAsync(
       int cityId,
       CancellationToken cancellationToken);

    Task<City?> GetByNameAndCountryAsync(
        string name,
        string countryCode,
        CancellationToken cancellationToken);
    Task<bool> ExistsWithNameAndCountryAsync(
    string name,
    string countryCode,
    int excludedCityId,
    CancellationToken cancellationToken);

    void Add(City city);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);

    
}