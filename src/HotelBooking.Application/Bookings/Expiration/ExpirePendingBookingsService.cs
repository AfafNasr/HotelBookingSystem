using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.Expiration;

public sealed class ExpirePendingBookingsService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingConcurrencyManager _bookingConcurrencyManager;

    public ExpirePendingBookingsService(
        IBookingRepository bookingRepository,
        IBookingConcurrencyManager bookingConcurrencyManager)
    {
        _bookingRepository = bookingRepository;
        _bookingConcurrencyManager = bookingConcurrencyManager;
    }

    public async Task<int> ExecuteAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var bookingIds =
            await _bookingRepository.GetExpiredPendingBookingIdsAsync(
                now,
                cancellationToken);

        var expiredCount = 0;

        foreach (var bookingId in bookingIds)
        {
            var expired =
                await _bookingConcurrencyManager
                    .ExecuteWithBookingLockAsync(
                        bookingId,
                        async ct =>
                        {
                            var booking =
                                await _bookingRepository.GetByIdAsync(
                                    bookingId,
                                    ct);

                            if (booking is null)
                            {
                                return false;
                            }

                            var shouldExpire =
                                booking.Status ==
                                    BookingStatus.PendingPayment &&
                                booking.ExpiresAt is not null &&
                                booking.ExpiresAt <= now;

                            if (!shouldExpire)
                            {
                                return false;
                            }

                            booking.Expire(now);

                            await _bookingRepository.SaveChangesAsync(ct);

                            return true;
                        },
                        cancellationToken);

            if (expired)
            {
                expiredCount++;
            }
        }

        return expiredCount;
    }
}