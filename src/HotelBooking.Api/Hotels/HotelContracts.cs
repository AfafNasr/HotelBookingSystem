using HotelBooking.Domain.Hotels;

namespace HotelBooking.Api.Hotels;

public sealed record CreateHotelRequest(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category);

public sealed record CreateHotelResponse(
    int HotelId);

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