namespace HotelBooking.Application.Hotels.GetNearbyAttractions;

public sealed record GetNearbyAttractionsQuery(
    int HotelId,
    int RadiusMeters = 3000,
    int Limit = 10);