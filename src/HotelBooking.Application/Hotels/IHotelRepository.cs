using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels;

public interface IHotelRepository
{
    Task<Hotel?> GetByIdAsync(
        int hotelId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveOrUpcomingBookingsAsync(
    int hotelId,
    DateOnly today,
    DateTime now,
    CancellationToken cancellationToken);

    void Add(Hotel hotel);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}