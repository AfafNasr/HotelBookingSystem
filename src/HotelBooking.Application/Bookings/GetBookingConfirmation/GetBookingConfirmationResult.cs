using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public sealed record GetBookingConfirmationResult(
    bool Succeeded,
    BookingConfirmation? Confirmation,
    IReadOnlyCollection<ApplicationError> Errors);