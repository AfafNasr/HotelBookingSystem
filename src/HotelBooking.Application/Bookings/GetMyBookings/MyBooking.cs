using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.GetMyBookings;

public sealed record MyBooking(
    int BookingId,
    string HotelName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    decimal TotalAmount,
    BookingStatus Status,
    string? ConfirmationNumber,
    DateTime CreatedAt);