namespace HotelBooking.Application.HotelAmenities.AddHotelAmenity;

public sealed record AddHotelAmenityCommand(
    int HotelId,
    int AmenityId);