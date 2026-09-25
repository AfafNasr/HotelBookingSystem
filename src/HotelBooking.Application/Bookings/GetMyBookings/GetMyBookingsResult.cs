using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Bookings.GetMyBookings;

public sealed record GetMyBookingsResult(
    bool Succeeded,
    IReadOnlyCollection<MyBooking> Bookings,
    IReadOnlyCollection<ApplicationError> Errors);