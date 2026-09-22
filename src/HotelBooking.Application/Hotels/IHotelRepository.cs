using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels;

public interface IHotelRepository
{
    Task<Hotel?> GetByIdAsync(
        int hotelId,
        CancellationToken cancellationToken);

    void Add(Hotel hotel);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}