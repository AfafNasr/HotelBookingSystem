namespace HotelBooking.Application.Common.Payments;

public sealed record CreatePaymentIntentResult(
    string ProviderPaymentIntentId,
    string ClientSecret);
