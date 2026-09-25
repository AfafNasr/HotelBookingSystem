using HotelBooking.Domain.Deals;

namespace HotelBooking.Application.Deals;

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(
     int dealId,
     CancellationToken cancellationToken);

    Task<bool> HasOverlappingDealExceptAsync(
    int hotelId,
    DateOnly startDate,
    DateOnly endDate,
    int excludedDealId,
    CancellationToken cancellationToken);


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

    void Remove(Deal deal);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}