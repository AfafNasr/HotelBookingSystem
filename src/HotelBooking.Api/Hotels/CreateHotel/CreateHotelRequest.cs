using HotelBooking.Domain.Hotels;

namespace HotelBooking.Api.Hotels.CreateHotel;

public sealed record CreateHotelRequest(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category);