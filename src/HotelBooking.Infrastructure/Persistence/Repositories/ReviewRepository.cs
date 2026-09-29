using HotelBooking.Application.Reviews;
using HotelBooking.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReviewRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Reviews.AnyAsync(
            review => review.BookingId == bookingId,
            cancellationToken);
    }

    public async Task<Review?> GetByIdForUserAsync(
    int reviewId,
    string userId,
    CancellationToken cancellationToken)
    {
        return await _dbContext.Reviews
            .FirstOrDefaultAsync(
                review =>
                    review.Id == reviewId &&
                    review.Booking.UserId == userId,
                cancellationToken);
    }

    public void Remove(Review review)
    {
        _dbContext.Reviews.Remove(review);
    }

    public void Add(Review review)
    {
        _dbContext.Reviews.Add(review);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}