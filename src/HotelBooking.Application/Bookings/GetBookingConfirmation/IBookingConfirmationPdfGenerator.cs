namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public interface IBookingConfirmationPdfGenerator
{
    byte[] Generate(BookingConfirmation confirmation);
}