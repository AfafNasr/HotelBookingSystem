namespace HotelBooking.Api.Payments;

public sealed record StartPaymentResponse(
    int PaymentId,
    string ClientSecret);