using HotelBooking.Application.Reviews.GetMyReviews;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class GetMyReviewsQuery : IGetMyReviewsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public GetMyReviewsQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MyReview>> GetAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.Booking.UserId == userId)
            .OrderByDescending(review =>
                review.CreatedAt)
            .Select(review => new MyReview(
                review.Id,
                review.BookingId,
                review.Booking.HotelId,
                review.Booking.Hotel.Name,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}