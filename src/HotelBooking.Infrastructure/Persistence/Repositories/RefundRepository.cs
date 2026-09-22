using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class RefundRepository : IRefundRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RefundRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Refund?> GetByPaymentIdAsync(
        int paymentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Refunds
            .SingleOrDefaultAsync(
                refund => refund.PaymentId == paymentId,
                cancellationToken);
    }

    public void Add(Refund refund)
    {
        _dbContext.Refunds.Add(refund);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}