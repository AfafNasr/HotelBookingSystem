using HotelBooking.Domain.Amenities;
using System.Xml.Linq;

namespace HotelBooking.Application.Amenities;

public interface IAmenityRepository
{
    Task<bool> ExistsByIdAsync(
    int amenityId,
    CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<Amenity?> GetByIdAsync(
       int amenityId,
       CancellationToken cancellationToken);

    Task<bool> ExistsByNameExceptAsync(
    string name,
    int excludedAmenityId,
    CancellationToken cancellationToken);

    void Add(Amenity amenity);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
