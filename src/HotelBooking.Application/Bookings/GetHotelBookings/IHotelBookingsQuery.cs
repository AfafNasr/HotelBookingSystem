using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.GetHotelBookings;

public interface IHotelBookingsQuery
{
    Task<HotelBookingsPage> GetAsync(
        int hotelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record HotelBookingsPage(
    IReadOnlyCollection<HotelBookingItem> Bookings,
    bool HasNextPage);

public sealed record HotelBookingItem(
    int BookingId,
    string GuestFullName,
    string GuestEmail,
    string? GuestPhoneNumber,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int NumberOfRooms,
    decimal TotalAmount,
    BookingStatus Status,
    string? ConfirmationNumber,
    DateTime CreatedAt);