namespace HotelBooking.Api.Hotels.SearchHotels;

public sealed record SearchHotelsRequest(
    string Destination,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms,
    int Page = 1,
    int PageSize = 20);