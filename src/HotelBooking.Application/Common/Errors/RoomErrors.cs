namespace HotelBooking.Application.Common.Errors;

public static class RoomErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Room.NotFound",
            "The specified room does not exist.",
            ErrorType.NotFound);

    public static readonly ApplicationError NumberAlreadyExists =
        new(
            "Room.NumberAlreadyExists",
            "A room with this number already exists in the hotel.",
            ErrorType.Conflict);

    public static readonly ApplicationError HasActiveBookings =
        new(
            "Room.HasActiveBookings",
            "The room cannot be deleted because it has active or upcoming bookings.",
            ErrorType.Conflict);
}