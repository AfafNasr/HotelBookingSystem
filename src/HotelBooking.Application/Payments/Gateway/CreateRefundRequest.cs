namespace HotelBooking.Application.Payments.Gateway;

public sealed record CreateRefundRequest(
    int RefundId,
    string ProviderPaymentIntentId,
    decimal Amount);