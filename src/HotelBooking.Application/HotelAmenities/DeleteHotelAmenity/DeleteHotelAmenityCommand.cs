namespace HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;

public sealed record DeleteHotelAmenityCommand(
    int HotelId,
    int AmenityId);