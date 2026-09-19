using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.CreateHotel;

public sealed record CreateHotelCommand(
    string Name,
    int CityId,
    string OwnerId,
    int StarRating,
    HotelCategory Category);