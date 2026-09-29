namespace HotelBooking.Application.Common.Errors;

public static class CartErrors
{
    public static readonly ApplicationError RoomNotFound =
        new(
            "Cart.RoomNotFound",
            "The specified room does not exist.",
            ErrorType.NotFound);

    public static readonly ApplicationError DifferentHotel =
        new(
            "Cart.DifferentHotel",
            "The cart already contains rooms from another hotel.",
            ErrorType.Conflict);
}