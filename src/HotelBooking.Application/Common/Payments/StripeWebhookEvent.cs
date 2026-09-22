namespace HotelBooking.Application.Common.Payments;

public sealed record StripeWebhookEvent(
    string EventId,
    string EventType,
    string? ProviderPaymentIntentId);