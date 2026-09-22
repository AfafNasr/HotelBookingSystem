using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Bookings;

internal static class BookingLog
{
    private static readonly Action<ILogger, int, string, int, int, Exception?>
        BookingCreatedMessage =
            LoggerMessage.Define<int, string, int, int>(
                LogLevel.Information,
                new EventId(1001, nameof(BookingCreated)),
                "Booking {BookingId} was created by user {UserId} for hotel {HotelId} with {RoomCount} room(s).");

    private static readonly Action<ILogger, string, int, int, Exception?>
        RoomsUnavailableMessage =
            LoggerMessage.Define<string, int, int>(
                LogLevel.Warning,
                new EventId(1002, nameof(RoomsUnavailable)),
                "Booking creation failed for user {UserId} at hotel {HotelId} because {UnavailableRoomCount} requested room(s) are unavailable.");

    public static void BookingCreated(
        ILogger logger,
        int bookingId,
        string userId,
        int hotelId,
        int roomCount)
    {
        BookingCreatedMessage(
            logger,
            bookingId,
            userId,
            hotelId,
            roomCount,
            null);
    }

    public static void RoomsUnavailable(
        ILogger logger,
        string userId,
        int hotelId,
        int unavailableRoomCount)
    {
        RoomsUnavailableMessage(
            logger,
            userId,
            hotelId,
            unavailableRoomCount,
            null);
    }
}