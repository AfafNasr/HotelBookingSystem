namespace HotelBooking.Application.Common.Errors;

public static class BookingErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Booking.NotFound",
            "The booking was not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError AccessDenied =
        new(
            "Booking.AccessDenied",
            "You are not allowed to access this booking.",
            ErrorType.Authorization);

    public static readonly ApplicationError NotPendingPayment =
        new(
            "Booking.NotPendingPayment",
            "Only a pending payment booking can be paid.",
            ErrorType.Conflict);

    public static readonly ApplicationError PaymentHoldExpired =
        new(
            "Booking.PaymentHoldExpired",
            "The booking payment hold has expired.",
            ErrorType.Conflict);

    public static readonly ApplicationError ConfirmationNotReady =
        new(
            "Booking.ConfirmationNotReady",
            "The booking confirmation is not available until payment succeeds.",
            ErrorType.Conflict);

    public static readonly ApplicationError RoomNotFound =
        new(
            "Booking.RoomNotFound",
            "One or more selected rooms were not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError RoomHotelMismatch =
        new(
            "Booking.RoomHotelMismatch",
            "All selected rooms must belong to the selected hotel.",
            ErrorType.Validation);

    public static ApplicationError RoomsUnavailable(
        IEnumerable<int> roomIds) =>
        new(
            "Booking.RoomUnavailable",
            $"One or more selected rooms are unavailable: {string.Join(", ", roomIds)}.",
            ErrorType.Conflict);

    public static readonly ApplicationError PaymentAccessDenied =
    new(
        "Booking.PaymentAccessDenied",
        "You are not allowed to pay for this booking.",
        ErrorType.Authorization);

    public static readonly ApplicationError CannotCancel =
    new(
        "Booking.CannotCancel",
        "Only a pending payment booking can be cancelled.",
        ErrorType.Conflict);

    public static readonly ApplicationError CannotUpdate =
    new(
        "Booking.CannotUpdate",
        "Only a pending payment booking can be updated.",
        ErrorType.Conflict);
}