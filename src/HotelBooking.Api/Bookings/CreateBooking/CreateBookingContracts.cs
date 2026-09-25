using HotelBooking.Domain.Bookings;

namespace HotelBooking.Api.Bookings.CreateBooking;

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