namespace HotelBooking.Api.Reviews;

public sealed record CreateReviewRequest(
    int Rating,
    string? Comment);

public sealed record CreateReviewResponse(
    int ReviewId);