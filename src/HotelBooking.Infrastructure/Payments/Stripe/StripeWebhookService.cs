using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Payments.Stripe;

public sealed class StripeWebhookService : IStripeWebhookService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IOptions<StripeOptions> options,
          ILogger<StripeWebhookService> logger
        )
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool TryConstructEvent(
        string json,
        string signature,
        out StripeWebhookEvent? webhookEvent)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
    json,
    signature,
    _options.WebhookSecret,
    throwOnApiVersionMismatch: false);

            var paymentIntent =
                stripeEvent.Data.Object as PaymentIntent;

            webhookEvent = new StripeWebhookEvent(
                stripeEvent.Id,
                stripeEvent.Type,
                paymentIntent?.Id);

            return true;
        }
        catch (StripeException exception)
        {
            _logger.LogWarning(
                exception,
                "Stripe webhook signature verification failed.");

            webhookEvent = null;
            return false;
        }
    }
}