using HotelBooking.Domain.Payments;

namespace HotelBooking.Application.Common.Interfaces;

public interface IRefundRepository
{
    Task<Refund?> GetByPaymentIdAsync(
        int paymentId,
        CancellationToken cancellationToken);

    void Add(Refund refund);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}