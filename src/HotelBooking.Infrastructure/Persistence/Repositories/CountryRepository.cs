using HotelBooking.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class CountryRepository : ICountryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CountryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        string countryCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.Countries
            .AsNoTracking()
            .AnyAsync(
                country => country.Code == countryCode,
                cancellationToken);
    }
}