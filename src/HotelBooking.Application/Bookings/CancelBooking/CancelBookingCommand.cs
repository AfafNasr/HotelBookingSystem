namespace HotelBooking.Application.Bookings.CancelBooking;

public sealed record CancelBookingCommand(
    int BookingId);