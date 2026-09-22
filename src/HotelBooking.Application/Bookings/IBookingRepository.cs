using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings;

public interface IBookingRepository
{
    Task<IReadOnlyCollection<int>> GetUnavailableRoomIdsAsync(
        IReadOnlyCollection<int> roomIds,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<int>> GetExpiredPendingBookingIdsAsync(
      DateTime now,
      CancellationToken cancellationToken);

    Task<Booking?> GetByIdAsync(
    int bookingId,
    CancellationToken cancellationToken);

    void Add(Booking booking);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}