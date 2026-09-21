namespace HotelBooking.Application.Bookings.CreateBooking;

public sealed record CreateBookingCommand(
    int HotelId,
    IReadOnlyCollection<int> RoomIds,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    string GuestFullName,
    string GuestEmail,
    string GuestPhoneNumber,
    string? SpecialRequests);