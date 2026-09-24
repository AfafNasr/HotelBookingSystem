using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public sealed record GetBookingConfirmationResult(
    bool Succeeded,
    BookingConfirmation? Confirmation,
    IReadOnlyCollection<ApplicationError> Errors);