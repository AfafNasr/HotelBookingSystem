namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public interface IBookingConfirmationQuery
{
    Task<BookingConfirmation?> GetAsync(
        int bookingId,
        CancellationToken cancellationToken);
}