using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Reviews.UpdateReview;

public sealed record UpdateReviewResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);