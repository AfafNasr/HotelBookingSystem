using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Cities;
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

    public async Task<IReadOnlyList<Country>> GetAllAsync(
    CancellationToken cancellationToken)
    {
        return await _dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .ToListAsync(cancellationToken);
    }
}