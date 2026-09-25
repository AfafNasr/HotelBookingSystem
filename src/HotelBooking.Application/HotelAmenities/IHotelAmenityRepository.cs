using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.HotelAmenities;

public interface IHotelAmenityRepository
{
    Task<bool> ExistsAsync(
        int hotelId,
        int amenityId,
        CancellationToken cancellationToken);

    Task<HotelAmenity?> GetAsync(
    int hotelId,
    int amenityId,
    CancellationToken cancellationToken);

    void Remove(HotelAmenity hotelAmenity);

    void Add(HotelAmenity hotelAmenity);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
