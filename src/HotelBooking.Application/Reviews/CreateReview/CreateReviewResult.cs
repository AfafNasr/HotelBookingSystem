using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Reviews.CreateReview;

public sealed record CreateReviewResult(
    bool Succeeded,
    int? ReviewId,
    IReadOnlyCollection<ApplicationError> Errors);