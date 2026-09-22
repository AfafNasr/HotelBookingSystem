using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Bookings.CreateBooking;

public sealed record CreateBookingResult(
    bool Succeeded,
    int? BookingId,
    IReadOnlyCollection<ApplicationError> Errors);