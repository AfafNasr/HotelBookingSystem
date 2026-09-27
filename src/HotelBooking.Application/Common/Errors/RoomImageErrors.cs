namespace HotelBooking.Application.Common.Errors;

public static class RoomImageErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "RoomImage.NotFound",
            "The room image was not found.",
            ErrorType.NotFound);
}