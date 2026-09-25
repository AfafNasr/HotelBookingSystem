using HotelBooking.Application.Hotels.GetAdminHotelById;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class AdminHotelByIdQuery : IAdminHotelByIdQuery
{
    private readonly ApplicationDbContext _dbContext;

    public AdminHotelByIdQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AdminHotelDetails?> GetByIdAsync(
        int hotelId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Hotels
            .AsNoTracking()
            .Where(hotel =>
                hotel.Id == hotelId &&
                !hotel.IsDeleted)
            .Select(hotel => new AdminHotelDetails(
                hotel.Id,
                hotel.Name,
                hotel.CityId,
                hotel.City.Name,
                hotel.OwnerId,

                _dbContext.Users
                    .Where(user => user.Id == hotel.OwnerId)
                    .Select(user => user.UserName!)
                    .FirstOrDefault() ?? "Unknown",

                hotel.StarRating,
                hotel.Category,
                hotel.Description,
                hotel.Address,
                hotel.Latitude,
                hotel.Longitude,
                hotel.CreatedAt,
                hotel.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}