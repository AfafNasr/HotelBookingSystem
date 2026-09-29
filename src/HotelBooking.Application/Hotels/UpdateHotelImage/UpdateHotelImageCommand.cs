namespace HotelBooking.Application.Hotels.UpdateHotelImage;

public sealed record UpdateHotelImageCommand(
    int HotelId,
    int ImageId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length);