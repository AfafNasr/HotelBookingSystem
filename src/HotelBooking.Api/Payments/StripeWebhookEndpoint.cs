using HotelBooking.Api.Common;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Payments.HandleStripeWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Payments;

[ApiController]
[Route("api/payments/stripe/webhook")]
public sealed class StripeWebhookEndpoint : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;
    private readonly HandleStripeWebhookCommandHandler _handler;

    public StripeWebhookEndpoint(
        IStripeWebhookService stripeWebhookService,
        HandleStripeWebhookCommandHandler handler)
    {
        _stripeWebhookService = stripeWebhookService;
        _handler = handler;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);

        var json = await reader.ReadToEndAsync(
            cancellationToken);

        var signature =
            Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
        {
            return BadRequest();
        }

        if (!_stripeWebhookService.TryConstructEvent(
                json,
                signature,
                out var webhookEvent) ||
            webhookEvent is null)
        {
            return BadRequest();
        }

        var command = new HandleStripeWebhookCommand(
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ProviderPaymentIntentId);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok();
    }
}