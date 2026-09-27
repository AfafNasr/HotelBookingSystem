namespace HotelBooking.Application.Hotels.GetNearbyAttractions;

public sealed record NearbyAttraction(
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    int? DistanceMeters);