namespace HotelBooking.Domain.Payments;

public sealed class Refund
{
    public int Id { get; private set; }

    public int PaymentId { get; private set; }

    public string? ProviderRefundId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public RefundStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private Refund()
    {
    }

    public Refund(
        int paymentId,
        decimal amount,
        string currency,
        DateTime createdAt)
    {
        if (paymentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paymentId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        PaymentId = paymentId;
        Amount = amount;
        Currency = currency.Trim().ToLowerInvariant();
        Status = RefundStatus.Pending;
        CreatedAt = createdAt;
    }

    public void MarkSucceeded(
        string providerRefundId,
        DateTime succeededAt)
    {
        if (string.IsNullOrWhiteSpace(providerRefundId))
        {
            throw new ArgumentException(
                "Provider refund ID is required.",
                nameof(providerRefundId));
        }

        var normalizedProviderRefundId = providerRefundId.Trim();

        if (Status == RefundStatus.Succeeded)
        {
            if (ProviderRefundId == normalizedProviderRefundId)
            {
                return;
            }

            throw new InvalidOperationException(
                "A different provider refund is already attached.");
        }

        if (Status != RefundStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending refund can succeed.");
        }

        ProviderRefundId = normalizedProviderRefundId;
        Status = RefundStatus.Succeeded;
        UpdatedAt = succeededAt;
    }

    public void MarkFailed(DateTime failedAt)
    {
        if (Status == RefundStatus.Failed)
        {
            return;
        }

        if (Status != RefundStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending refund can fail.");
        }

        Status = RefundStatus.Failed;
        UpdatedAt = failedAt;
    }
}