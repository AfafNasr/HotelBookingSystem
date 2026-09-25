namespace HotelBooking.Application.Bookings;

public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    public int PaymentHoldDurationMinutes { get; init; } = 15;
}