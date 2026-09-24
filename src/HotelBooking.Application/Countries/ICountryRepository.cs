using HotelBooking.Domain.Cities;

namespace HotelBooking.Application.Countries;

public interface ICountryRepository
{
    Task<bool> ExistsAsync(
        string countryCode,
        CancellationToken cancellationToken );

    Task<IReadOnlyList<Country>> GetAllAsync(
        CancellationToken cancellationToken);
}