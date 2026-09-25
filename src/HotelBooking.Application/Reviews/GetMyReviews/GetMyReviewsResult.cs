using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Reviews.GetMyReviews;

public sealed record GetMyReviewsResult(
    bool Succeeded,
    IReadOnlyCollection<MyReview> Reviews,
    IReadOnlyCollection<ApplicationError> Errors);