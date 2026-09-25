using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Reviews;

public static class ReviewErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Review.NotFound",
            "The review was not found.",
            ErrorType.NotFound);


}