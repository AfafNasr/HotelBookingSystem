namespace HotelBooking.Application.Common.Interfaces;

public interface ICountryRepository
{
    Task<bool> ExistsAsync(
        string countryCode,
        CancellationToken cancellationToken );
}