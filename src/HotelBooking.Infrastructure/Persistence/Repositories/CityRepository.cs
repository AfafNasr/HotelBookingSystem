using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Cities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class CityRepository : ICityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CityRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<City?> GetByNameAndCountryAsync(
        string name,
        string countryCode,
        CancellationToken cancellationToken)
    {
        return _dbContext.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                city =>
                    city.Name == name &&
                    city.CountryCode == countryCode,
                cancellationToken);
    }

    public void Add(City city)
    {
        _dbContext.Cities.Add(city);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}