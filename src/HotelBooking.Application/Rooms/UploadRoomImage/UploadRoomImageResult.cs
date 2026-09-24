using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Rooms.UploadRoomImage;

public sealed record UploadRoomImageResult(
    bool Succeeded,
    int? ImageId,
    IReadOnlyCollection<ApplicationError> Errors);