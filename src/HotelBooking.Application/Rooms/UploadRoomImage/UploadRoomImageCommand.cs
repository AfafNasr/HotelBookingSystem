namespace HotelBooking.Application.Rooms.UploadRoomImage;

public sealed record UploadRoomImageCommand(
    int RoomId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length);