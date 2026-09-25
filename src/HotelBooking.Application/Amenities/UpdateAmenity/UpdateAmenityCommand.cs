namespace HotelBooking.Application.Amenities.UpdateAmenity;

public sealed record UpdateAmenityCommand(
    int AmenityId,
    string Name);