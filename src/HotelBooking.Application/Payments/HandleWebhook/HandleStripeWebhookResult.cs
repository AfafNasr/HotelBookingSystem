using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Payments.HandleStripeWebhook;

public sealed record HandleStripeWebhookResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);