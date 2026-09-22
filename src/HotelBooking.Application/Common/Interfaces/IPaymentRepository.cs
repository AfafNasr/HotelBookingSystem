using HotelBooking.Domain.Payments;

namespace HotelBooking.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken);

    Task<Payment?> GetByProviderPaymentIntentIdAsync(
    string providerPaymentIntentId,
    CancellationToken cancellationToken);

    Task<int?> GetBookingIdByProviderPaymentIntentIdAsync(
        string providerPaymentIntentId,
        CancellationToken cancellationToken);

    void Add(Payment payment);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}