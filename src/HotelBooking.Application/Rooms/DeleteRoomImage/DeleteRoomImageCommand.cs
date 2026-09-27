namespace HotelBooking.Application.Rooms.DeleteRoomImage;

public sealed record DeleteRoomImageCommand(
    int RoomId,
    int ImageId);