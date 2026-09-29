namespace HotelBooking.Application.Reviews.GetMyReviews;

public interface IGetMyReviewsQuery
{
    Task<IReadOnlyCollection<MyReview>> GetAsync(
        string userId,
        CancellationToken cancellationToken);
}

public sealed record MyReview(
    int ReviewId,
    int BookingId,
    int HotelId,
    string HotelName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);