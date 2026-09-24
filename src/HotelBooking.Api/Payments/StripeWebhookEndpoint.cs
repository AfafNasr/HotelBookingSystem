using HotelBooking.Api.Common;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application.Payments;
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
    private readonly ILogger<StripeWebhookEndpoint> _logger;

    public StripeWebhookEndpoint(
     IStripeWebhookService stripeWebhookService,
     HandleStripeWebhookCommandHandler handler,
     ILogger<StripeWebhookEndpoint> logger)
    {
        _stripeWebhookService = stripeWebhookService;
        _handler = handler;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
     CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stripe webhook HTTP request received.");

        using var reader = new StreamReader(Request.Body);

        var json = await reader.ReadToEndAsync(
            cancellationToken);

        _logger.LogInformation(
            "Stripe webhook request body read successfully. Content length: {ContentLength}.",
            json.Length);

        var signature =
            Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning(
                "Stripe webhook rejected because the Stripe-Signature header is missing.");

            return BadRequest();
        }

        _logger.LogInformation(
            "Stripe-Signature header was received.");

        if (!_stripeWebhookService.TryConstructEvent(
                json,
                signature,
                out var webhookEvent) ||
            webhookEvent is null)
        {
            _logger.LogWarning(
                "Stripe webhook rejected because event construction or signature validation failed.");

            return BadRequest();
        }

        _logger.LogInformation(
            "Stripe webhook validated successfully. EventId: {EventId}, EventType: {EventType}, PaymentIntentId: {PaymentIntentId}.",
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ProviderPaymentIntentId);

        var command = new HandleStripeWebhookCommand(
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.ProviderPaymentIntentId);

        _logger.LogInformation(
            "Sending Stripe event {EventId} to the payment webhook handler.",
            webhookEvent.EventId);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Stripe webhook handler failed for event {EventId}. Error count: {ErrorCount}.",
                webhookEvent.EventId,
                result.Errors.Count);

            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        _logger.LogInformation(
            "Stripe webhook event {EventId} processed successfully.",
            webhookEvent.EventId);

        return Ok();
    }
}