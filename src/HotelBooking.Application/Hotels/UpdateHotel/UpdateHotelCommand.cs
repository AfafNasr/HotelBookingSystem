using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.UpdateHotel;

public sealed record UpdateHotelCommand(
    int HotelId,
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);