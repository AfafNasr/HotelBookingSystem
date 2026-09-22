using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class HotelImageRepository
    : IHotelImageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HotelImageRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(HotelImage hotelImage)
    {
        _dbContext.HotelImages.Add(hotelImage);
    }

    public async Task<int> GetNextDisplayOrderAsync(
    int hotelId,
    CancellationToken cancellationToken)
    {
        var maxDisplayOrder = await _dbContext.HotelImages
            .Where(image => image.HotelId == hotelId)
            .Select(image => (int?)image.DisplayOrder)
            .MaxAsync(cancellationToken);

        return (maxDisplayOrder ?? -1) + 1;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}