namespace HotelBooking.Application.Hotels.UploadHotelImage;

public sealed record UploadHotelImageCommand(
    int HotelId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length);