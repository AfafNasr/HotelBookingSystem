namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed record SearchHotelsPage(
    IReadOnlyCollection<SearchHotelItem> Hotels,
    bool HasNextPage);