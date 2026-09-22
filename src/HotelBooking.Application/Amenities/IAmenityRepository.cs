using HotelBooking.Domain.Amenities;

namespace HotelBooking.Application.Amenities;

public interface IAmenityRepository
{
    Task<bool> ExistsByIdAsync(
    int amenityId,
    CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken);

    void Add(Amenity amenity);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
