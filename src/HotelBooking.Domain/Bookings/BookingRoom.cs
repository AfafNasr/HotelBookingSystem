using HotelBooking.Domain.Rooms;

namespace HotelBooking.Domain.Bookings;

public sealed class BookingRoom
{
    public int BookingId { get; private set; }
    public int RoomId { get; private set; }

    public decimal OriginalPricePerNight { get; private set; }

    public Booking Booking { get; private set; } = null!;
    public Room Room { get; private set; } = null!;

    private BookingRoom()
    {
    }

    internal BookingRoom(
        int roomId,
        decimal originalPricePerNight)
    {
        RoomId = roomId;
        OriginalPricePerNight = originalPricePerNight;
    }
}