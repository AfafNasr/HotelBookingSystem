using HotelBooking.Application.Amenities;
using HotelBooking.Domain.Amenities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class AmenityRepository : IAmenityRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AmenityRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Task<bool> ExistsByIdAsync(
     int amenityId,
     CancellationToken cancellationToken)
    {
        return _dbContext.Amenities.AnyAsync(
            amenity =>
                amenity.Id == amenityId &&
                !amenity.IsDeleted,
            cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();

        return _dbContext.Amenities
            .AnyAsync(
                amenity =>
                    !amenity.IsDeleted &&
                    amenity.Name == normalizedName,
                cancellationToken);
    }
    public async Task<Amenity?> GetByIdAsync(
    int amenityId,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Amenities
            .FirstOrDefaultAsync(
                amenity =>
                    amenity.Id == amenityId &&
                    !amenity.IsDeleted,
                cancellationToken);
    }

    public Task<bool> ExistsByNameExceptAsync(
    string name,
    int excludedAmenityId,
    CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();

        return _dbContext.Amenities.AnyAsync(
            amenity =>
                !amenity.IsDeleted &&
                amenity.Id != excludedAmenityId &&
                amenity.Name == normalizedName,
            cancellationToken);
    }

    public void Add(Amenity amenity)
    {
        _dbContext.Amenities.Add(amenity);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
