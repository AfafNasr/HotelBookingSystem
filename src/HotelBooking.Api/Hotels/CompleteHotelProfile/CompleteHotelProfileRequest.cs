namespace HotelBooking.Api.Hotels.CompleteHotelProfile;

public sealed record CompleteHotelProfileRequest(
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude);