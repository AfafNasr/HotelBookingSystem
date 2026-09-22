using HotelBooking.Domain.Reviews;

namespace HotelBooking.Application.Reviews;

public interface IReviewRepository
{
    Task<bool> ExistsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken);

    void Add(Review review);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}