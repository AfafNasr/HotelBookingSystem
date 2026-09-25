namespace HotelBooking.Application.Common.Errors;

public static class HotelErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Hotel.NotFound",
            "The specified hotel does not exist.",
            ErrorType.NotFound);

    public static readonly ApplicationError HasActiveBookings =
        new(
            "Hotel.HasActiveBookings",
            "The hotel cannot be deleted because it has active or upcoming bookings.",
            ErrorType.Conflict);

    public static readonly ApplicationError ManagementForbidden =
    new(
        "Hotel.ManagementForbidden",
        "You are not allowed to manage this hotel.",
        ErrorType.Authorization);
}