namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed record SearchHotelItem(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    string? Description,
    decimal StartingPricePerNight,
    string? ThumbnailStorageKey);