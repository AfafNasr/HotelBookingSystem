using HotelBooking.Application.Common.Models;
using HotelBooking.Domain.Bookings;

namespace HotelBooking.Api.Bookings;

public sealed record CreateBookingResponse(
    int BookingId,
    BookingStatus Status);
