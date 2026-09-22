namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(
    string EventId,
    string EventType,
    string? ProviderPaymentIntentId);