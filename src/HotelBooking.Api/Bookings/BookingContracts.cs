using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Domain.Rooms;

namespace HotelBooking.Api.Bookings;


public sealed record CreateBookingRequest(
    int HotelId,
    IReadOnlyCollection<int> RoomIds,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string GuestFullName,
    string GuestEmail,
    string GuestPhoneNumber,
    string? SpecialRequests);

public sealed record CreateBookingResponse(
    int BookingId,
    BookingStatus Status);

public sealed record GetBookingConfirmationResponse(
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

public sealed record GetMyBookingResponse(
    int BookingId,
    string HotelName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    decimal TotalAmount,
    BookingStatus Status,
    string? ConfirmationNumber,
    DateTime CreatedAt);