namespace HotelBooking.Api.Hotels.UploadHotelImage;

public sealed record UploadHotelImageRequest(
    IFormFile File);