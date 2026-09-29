using HotelBooking.Domain.Bookings;

namespace HotelBooking.Application.Bookings.GetMyBookings;

public interface IMyBookingsQuery
{
    Task<IReadOnlyCollection<MyBooking>> GetAsync(
        string userId,
        CancellationToken cancellationToken);
}

public sealed record MyBooking(
    int BookingId,
    string HotelName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    decimal TotalAmount,
    BookingStatus Status,
    string? ConfirmationNumber,
    DateTime CreatedAt);