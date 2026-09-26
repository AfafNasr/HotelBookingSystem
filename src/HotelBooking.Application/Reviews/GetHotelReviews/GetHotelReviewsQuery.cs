namespace HotelBooking.Application.Reviews.GetHotelReviews;

public sealed record GetHotelReviewsQuery(
    int HotelId,
    int Page = 1,
    int PageSize = 20);