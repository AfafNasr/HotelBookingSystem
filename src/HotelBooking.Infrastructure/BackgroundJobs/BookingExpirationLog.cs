using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.BackgroundJobs;

internal static class BookingExpirationLog
{
    private static readonly Action<ILogger, int, Exception?>
        BookingsExpiredMessage =
            LoggerMessage.Define<int>(
                LogLevel.Information,
                new EventId(3001, nameof(BookingsExpired)),
                "Expired {ExpiredBookingCount} pending bookings.");

    private static readonly Action<ILogger, Exception?>
        ExpirationCycleFailedMessage =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(3002, nameof(ExpirationCycleFailed)),
                "An error occurred while expiring pending bookings.");

    public static void BookingsExpired(
        ILogger logger,
        int expiredBookingCount)
    {
        BookingsExpiredMessage(
            logger,
            expiredBookingCount,
            null);
    }

    public static void ExpirationCycleFailed(
        ILogger logger,
        Exception exception)
    {
        ExpirationCycleFailedMessage(
            logger,
            exception);
    }
}