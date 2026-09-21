using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyCollection<int>> GetUnavailableRoomIdsAsync(
        IReadOnlyCollection<int> roomIds,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Booking>> GetExpiredPendingBookingsAsync(
    DateTime now,
    CancellationToken cancellationToken);

    void Add(Booking booking);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}