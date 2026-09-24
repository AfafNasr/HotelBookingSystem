namespace HotelBooking.Application.Payments.StartPayment;

public sealed record StripeWebhookEvent(
    string EventId,
    string EventType,
    string? ProviderPaymentIntentId);