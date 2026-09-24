using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Bookings.GetBookingConfirmation;

public sealed record BookingConfirmation(
    string UserId,
    BookingStatus BookingStatus,
    string? ConfirmationNumber,
    string HotelName,
    string? HotelAddress,
    string GuestFullName,
    string GuestEmail,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    IReadOnlyCollection<BookingConfirmationRoom> Rooms,
    decimal TotalAmount,
    PaymentStatus? PaymentStatus,
    decimal? PaymentAmount,
    string? Currency,
     int NumberOfNights = 0,
    decimal SubtotalAmount = 0,
    decimal DiscountAmount = 0);

public sealed record BookingConfirmationRoom(
    int RoomId,
    RoomType RoomType,
    string? Description,
    decimal OriginalPricePerNight);