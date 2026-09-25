using HotelBooking.Application.Payments.HandleWebhook;

namespace HotelBooking.Application.Payments;

public interface IStripeWebhookService
{
    bool TryConstructEvent(
        string json,
        string signature,
        out StripeWebhookEvent? webhookEvent);
}