namespace HotelBooking.Application.Hotels.DeleteHotelImage;

public sealed record DeleteHotelImageCommand(
    int HotelId,
    int ImageId);