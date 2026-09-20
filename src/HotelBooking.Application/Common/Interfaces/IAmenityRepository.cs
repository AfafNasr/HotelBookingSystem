using HotelBooking.Domain.Amenities;

namespace HotelBooking.Application.Common.Interfaces;

public interface IAmenityRepository
{
    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken);

    void Add(Amenity amenity);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
