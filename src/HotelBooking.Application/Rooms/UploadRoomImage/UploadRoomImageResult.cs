using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Rooms.UploadRoomImage;

public sealed record UploadRoomImageResult(
    bool Succeeded,
    int? ImageId,
    IReadOnlyCollection<ApplicationError> Errors);