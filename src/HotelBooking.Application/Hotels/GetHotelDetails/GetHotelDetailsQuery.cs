namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed record GetHotelDetailsQuery(
    int HotelId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms);