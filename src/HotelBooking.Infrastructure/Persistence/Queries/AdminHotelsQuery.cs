using HotelBooking.Application.Hotels.GetAdminHotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class AdminHotelsQuery : IAdminHotelsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public AdminHotelsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdminHotel>> GetAsync(
        GetAdminHotelsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Hotels
            .AsNoTracking()
            .Where(hotel => !hotel.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(hotel =>
                hotel.Name.Contains(search));
        }

        var hotels = await query
            .OrderBy(hotel => hotel.Name)
            .Select(hotel => new AdminHotel(
                hotel.Id,
                hotel.Name,
                hotel.StarRating,

                _dbContext.Users
                    .Where(user => user.Id == hotel.OwnerId)
                    .Select(user => user.UserName!)
                    .FirstOrDefault() ?? "Unknown",

                _dbContext.Room
                    .Count(room =>
                        room.HotelId == hotel.Id &&
                        !room.IsDeleted),

                hotel.CreatedAt,
                hotel.UpdatedAt))
            .ToListAsync(cancellationToken);

        return hotels;
    }
}