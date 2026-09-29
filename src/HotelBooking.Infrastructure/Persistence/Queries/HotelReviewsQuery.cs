using HotelBooking.Application.Reviews.GetHotelReviews;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class HotelReviewsQuery : IHotelReviewsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public HotelReviewsQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HotelReviewsPage> GetAsync(
        int hotelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.Booking.HotelId == hotelId)
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(review => new HotelReviewItem(
                review.Id,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = reviews.Count > pageSize;

        if (hasNextPage)
        {
            reviews.RemoveAt(reviews.Count - 1);
        }

        return new HotelReviewsPage(
            reviews,
            hasNextPage);
    }
}