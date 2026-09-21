using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.UploadHotelImage;

public sealed record UploadHotelImageResult(
    bool Succeeded,
    int? ImageId,
    IReadOnlyCollection<ApplicationError> Errors);