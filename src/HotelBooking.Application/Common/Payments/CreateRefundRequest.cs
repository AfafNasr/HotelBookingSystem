namespace HotelBooking.Application.Common.Payments;

public sealed record CreateRefundRequest(
    int RefundId,
    string ProviderPaymentIntentId,
    decimal Amount);