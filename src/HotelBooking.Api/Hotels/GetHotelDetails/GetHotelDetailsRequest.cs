namespace HotelBooking.Api.Hotels.GetHotelDetails;

public sealed record GetHotelDetailsRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms);