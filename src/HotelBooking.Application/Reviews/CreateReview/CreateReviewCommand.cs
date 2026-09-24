namespace HotelBooking.Application.Reviews.CreateReview;

public sealed record CreateReviewCommand(
    int BookingId,
    int Rating,
    string? Comment);