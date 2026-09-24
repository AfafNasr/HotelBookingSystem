namespace HotelBooking.Application.Hotels;

public interface IRecentlyVisitedHotelRepository
{
    Task RecordVisitAsync(
        string userId,
        int hotelId,
        DateTime visitedAt,
        CancellationToken cancellationToken);
}