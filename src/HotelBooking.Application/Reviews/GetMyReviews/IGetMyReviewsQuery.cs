namespace HotelBooking.Application.Reviews.GetMyReviews;

public interface IGetMyReviewsQuery
{
    Task<IReadOnlyCollection<MyReview>> GetAsync(
        string userId,
        CancellationToken cancellationToken);
}