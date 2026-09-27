namespace HotelBooking.Application.Common.Errors;

public static class HotelImageErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "HotelImage.NotFound",
            "The hotel image was not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError HotelMismatch =
        new(
            "HotelImage.HotelMismatch",
            "The image does not belong to the specified hotel.",
            ErrorType.NotFound);
}