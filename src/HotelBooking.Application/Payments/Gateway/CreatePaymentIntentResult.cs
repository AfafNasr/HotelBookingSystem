namespace HotelBooking.Application.Payments.Gateway;

public sealed record CreatePaymentIntentResult(
    string ProviderPaymentIntentId,
    string ClientSecret);
