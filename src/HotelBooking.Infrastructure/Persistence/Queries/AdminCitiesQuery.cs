using HotelBooking.Application.Cities.GetAdminCities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class AdminCitiesQuery : IAdminCitiesQuery
{
    private readonly ApplicationDbContext _dbContext;

    public AdminCitiesQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdminCity>> GetAsync(
        GetAdminCitiesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Cities
            .AsNoTracking()
            .Where(city => !city.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(city =>
                city.Name.Contains(search));
        }

        var cities = await query
            .OrderBy(city => city.Name)
            .Select(city => new AdminCity(
                city.Id,
                city.Name,
                city.Country.Name,
                city.PostOffice,
                _dbContext.Hotels.Count(hotel =>
                    hotel.CityId == city.Id &&
                    !hotel.IsDeleted),
                city.CreatedAt,
                city.UpdatedAt))
            .ToListAsync(cancellationToken);

        return cities;
    }
}