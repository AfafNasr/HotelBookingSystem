namespace HotelBooking.Application.Reviews.UpdateReview;

public sealed record UpdateReviewCommand(
    int ReviewId,
    int Rating,
    string? Comment);