using HotelBooking.Application.Payments.StartPayment;

namespace HotelBooking.Application.Payments;

public interface IStripeWebhookService
{
    bool TryConstructEvent(
        string json,
        string signature,
        out StripeWebhookEvent? webhookEvent);
}