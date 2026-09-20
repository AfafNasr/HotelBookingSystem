using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class HotelAmenityRepository
    : IHotelAmenityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HotelAmenityRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        int hotelId,
        int amenityId,
        CancellationToken cancellationToken)
    {
        return _dbContext.HotelAmenities.AnyAsync(
            hotelAmenity =>
                hotelAmenity.HotelId == hotelId &&
                hotelAmenity.AmenityId == amenityId,
            cancellationToken);
    }

    public void Add(HotelAmenity hotelAmenity)
    {
        _dbContext.HotelAmenities.Add(hotelAmenity);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}