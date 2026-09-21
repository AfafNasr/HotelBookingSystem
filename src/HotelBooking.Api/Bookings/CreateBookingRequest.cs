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