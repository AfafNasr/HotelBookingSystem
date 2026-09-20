namespace HotelBooking.Application.Hotels.CompleteHotelProfile;

public sealed record CompleteHotelProfileCommand(
    int HotelId,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);