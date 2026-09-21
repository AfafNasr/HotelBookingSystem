using HotelBooking.Application.Common.Interfaces;

namespace HotelBooking.Application.Bookings.Expiration;

public sealed class ExpirePendingBookingsService
{
    private readonly IBookingRepository _bookingRepository;

    public ExpirePendingBookingsService(
        IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<int> ExecuteAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var bookings =
            await _bookingRepository.GetExpiredPendingBookingsAsync(
                now,
                cancellationToken);

        if (bookings.Count == 0)
        {
            return 0;
        }

        foreach (var booking in bookings)
        {
            booking.Expire(now);
        }

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return bookings.Count;
    }
}