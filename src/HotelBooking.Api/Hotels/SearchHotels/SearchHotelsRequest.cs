using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.Api.Hotels.SearchHotels;

public sealed record SearchHotelsRequest(
    string Destination,
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