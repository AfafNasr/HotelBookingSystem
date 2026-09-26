namespace HotelBooking.Application.Reviews.GetHotelReviews;

public interface IHotelReviewsQuery
{
    Task<HotelReviewsPage> GetAsync(
        int hotelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record HotelReviewsPage(
    IReadOnlyCollection<HotelReviewItem> Reviews,
    bool HasNextPage);

public sealed record HotelReviewItem(
    int ReviewId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);