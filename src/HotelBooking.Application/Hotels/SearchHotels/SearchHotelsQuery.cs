using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed record SearchHotelsQuery(
    string Destination, // City Or Hotel
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms,
     decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? StarRating = null,
    HotelCategory? Category = null,
    IReadOnlyCollection<int>? AmenityIds = null,
    HotelSearchSort SortBy = HotelSearchSort.Name,
    int Page = 1,
    int PageSize = 20);