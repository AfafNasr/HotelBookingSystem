namespace HotelBooking.Application.Payments.Gateway;

public sealed record CreateRefundResult(
    string ProviderRefundId);