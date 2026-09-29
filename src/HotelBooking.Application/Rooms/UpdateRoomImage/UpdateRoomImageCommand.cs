namespace HotelBooking.Application.Rooms.UpdateRoomImage;

public sealed record UpdateRoomImageCommand(
    int RoomId,
    int ImageId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length);