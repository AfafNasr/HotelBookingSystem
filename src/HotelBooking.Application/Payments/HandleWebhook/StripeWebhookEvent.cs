namespace HotelBooking.Application.Payments.HandleWebhook;

public sealed record StripeWebhookEvent(
    string EventId,
    string EventType,
    string? ProviderPaymentIntentId);