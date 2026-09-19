using HotelBooking.Domain.Cities;

namespace HotelBooking.Application.Common.Interfaces;

public interface ICountryRepository
{
    Task<bool> ExistsAsync(
        string countryCode,
        CancellationToken cancellationToken );

    Task<IReadOnlyList<Country>> GetAllAsync(
        CancellationToken cancellationToken);
}