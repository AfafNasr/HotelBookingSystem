namespace HotelBooking.Api.Hotels.SearchHotels;

public sealed record SearchHotelsResponse(
    IReadOnlyCollection<SearchHotelResponse> Hotels,
    int Page,
    int PageSize,
    bool HasNextPage);

public sealed record SearchHotelResponse(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    string? Description,
    decimal StartingPricePerNight,
    string? ThumbnailStorageKey);