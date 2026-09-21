namespace HotelBooking.Domain.Bookings;

public enum BookingStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}