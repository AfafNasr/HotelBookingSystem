using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Common.Interfaces;

public interface IHotelAmenityRepository
{
    Task<bool> ExistsAsync(
        int hotelId,
        int amenityId,
        CancellationToken cancellationToken);

    void Add(HotelAmenity hotelAmenity);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
