namespace HotelBooking.Domain.Payments;

public sealed class Payment
{
    public int Id { get; private set; }

    public int BookingId { get; private set; }

    public string? ProviderPaymentIntentId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public PaymentStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private Payment()
    {
    }

    public Payment(
    int bookingId,
    decimal amount,
    string currency,
    DateTime createdAt)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingId));
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

        BookingId = bookingId;
        Amount = amount;
        Currency = currency.Trim().ToLowerInvariant();

        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
    }

    public void AttachProviderPaymentIntent(
    string providerPaymentIntentId,
    DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentIntentId))
        {
            throw new ArgumentException(
                "Provider payment intent ID is required.",
                nameof(providerPaymentIntentId));
        }

        var normalizedPaymentIntentId =
            providerPaymentIntentId.Trim();

        if (ProviderPaymentIntentId is not null)
        {
            if (ProviderPaymentIntentId == normalizedPaymentIntentId)
            {
                return;
            }

            throw new InvalidOperationException(
                "A different provider payment intent is already attached.");
        }

        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                "A provider payment intent can only be attached to a pending payment.");
        }

        ProviderPaymentIntentId = normalizedPaymentIntentId;
        UpdatedAt = updatedAt;
    }

    public void MarkSucceeded(DateTime succeededAt)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending payment can be marked as succeeded.");
        }

        if (ProviderPaymentIntentId is null)
        {
            throw new InvalidOperationException(
                "A payment cannot succeed without a provider payment intent.");
        }

        Status = PaymentStatus.Succeeded;
        UpdatedAt = succeededAt;
    }
}