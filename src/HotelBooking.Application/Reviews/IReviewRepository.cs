using HotelBooking.Domain.Reviews;

namespace HotelBooking.Application.Reviews;

public interface IReviewRepository
{
    Task<bool> ExistsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken);

    Task<Review?> GetByIdForUserAsync(
    int reviewId,
    string userId,
    CancellationToken cancellationToken);

    void Remove(Review review);

    void Add(Review review);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}