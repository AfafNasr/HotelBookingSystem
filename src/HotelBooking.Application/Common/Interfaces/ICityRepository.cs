using HotelBooking.Domain.Cities;

namespace HotelBooking.Application.Common.Interfaces;

public interface ICityRepository
{
    Task<City?> GetByNameAndCountryAsync(
        string name,
        string countryCode,
        CancellationToken cancellationToken);

    void Add(City city);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}