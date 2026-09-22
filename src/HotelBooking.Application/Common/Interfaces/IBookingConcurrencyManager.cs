namespace HotelBooking.Application.Common.Interfaces;

public interface IBookingConcurrencyManager
{
    Task<T> ExecuteWithRoomLocksAsync<T>(
        IReadOnlyCollection<int> roomIds,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task<T> ExecuteWithBookingLockAsync<T>(
        int bookingId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}