namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed record SearchHotelsQuery(
    string Destination, // City Or Hotel
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children,
    int Rooms,
    int Page = 1,
    int PageSize = 20);