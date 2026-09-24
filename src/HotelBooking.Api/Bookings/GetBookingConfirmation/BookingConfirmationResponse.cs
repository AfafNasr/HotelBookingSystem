using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Bookings.GetBookingConfirmation;

public sealed record BookingConfirmationResponse(
    string ConfirmationNumber,
    string HotelName,
    string? HotelAddress,
    string GuestFullName,
    string GuestEmail,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int NumberOfNights,
    IReadOnlyCollection<BookingConfirmationRoomResponse> Rooms,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    PaymentStatus PaymentStatus,
    decimal PaymentAmount,
    string Currency);

public sealed record BookingConfirmationRoomResponse(
    int RoomId,
    RoomType RoomType,
    string? Description,
    decimal OriginalPricePerNight);