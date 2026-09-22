using HotelBooking.Application.Common.Payments;

namespace HotelBooking.Application.Common.Interfaces;

public interface IStripeWebhookService
{
    bool TryConstructEvent(
        string json,
        string signature,
        out StripeWebhookEvent? webhookEvent);
}