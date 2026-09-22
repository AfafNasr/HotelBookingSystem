namespace HotelBooking.Api.Reviews.CreateReview;

public sealed record CreateReviewRequest(
    int Rating,
    string? Comment);