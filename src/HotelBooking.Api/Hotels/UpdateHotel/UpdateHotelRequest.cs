using HotelBooking.Domain.Hotels;

namespace HotelBooking.Api.Hotels.UpdateHotel;

public sealed record UpdateHotelRequest(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);