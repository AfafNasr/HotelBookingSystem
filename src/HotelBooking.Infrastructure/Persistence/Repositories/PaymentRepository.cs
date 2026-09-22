using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Payments
            .SingleOrDefaultAsync(
                payment => payment.BookingId == bookingId,
                cancellationToken);
    }

    public Task<Payment?> GetByProviderPaymentIntentIdAsync(
    string providerPaymentIntentId,
    CancellationToken cancellationToken)
    {
        return _dbContext.Payments
            .SingleOrDefaultAsync(
                payment =>
                    payment.ProviderPaymentIntentId ==
                    providerPaymentIntentId,
                cancellationToken);
    }

    public Task<int?> GetBookingIdByProviderPaymentIntentIdAsync(
    string providerPaymentIntentId,
    CancellationToken cancellationToken)
    {
        return _dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.ProviderPaymentIntentId ==
                providerPaymentIntentId)
            .Select(payment => (int?)payment.BookingId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(Payment payment)
    {
        _dbContext.Payments.Add(payment);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}