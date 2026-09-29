namespace HotelBooking.Application.Hotels.SearchHotels;

public interface IHotelSearchQuery
{
    Task<SearchHotelsPage> SearchAsync(
        SearchHotelsQuery query,
        DateTime now,
        CancellationToken cancellationToken);
}

public sealed record SearchHotelsPage(
    IReadOnlyCollection<SearchHotelItem> Hotels,
    bool HasNextPage);