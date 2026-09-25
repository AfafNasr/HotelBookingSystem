namespace HotelBooking.Application.Bookings.GetMyBookings;

public interface IMyBookingsQuery
{
    Task<IReadOnlyCollection<MyBooking>> GetAsync(
        string userId,
        CancellationToken cancellationToken);
}