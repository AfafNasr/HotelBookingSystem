using HotelBooking.Domain.Deals;

namespace HotelBooking.Application.Deals;

public interface IDealRepository
{
    Task<bool> HasOverlappingDealAsync(
        int hotelId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Deal>> GetOverlappingDealsAsync(
        int hotelId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken);

    void Add(Deal deal);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}