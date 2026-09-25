using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Reviews.DeleteReview;

public sealed record DeleteReviewResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);