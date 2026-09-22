using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed record HandleStripeWebhookResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);