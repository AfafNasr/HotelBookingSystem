namespace HotelBooking.Application.Bookings.UpdateBooking;

public sealed record UpdateBookingCommand(
    int BookingId,
    string GuestFullName,
    string GuestEmail,
    string GuestPhoneNumber,
    string? SpecialRequests);