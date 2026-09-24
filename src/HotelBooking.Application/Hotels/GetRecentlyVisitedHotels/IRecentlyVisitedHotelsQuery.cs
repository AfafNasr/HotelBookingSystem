namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public interface IRecentlyVisitedHotelsQuery
{
    Task<IReadOnlyCollection<RecentlyVisitedHotelItem>> GetAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken);
}