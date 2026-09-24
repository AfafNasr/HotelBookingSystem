namespace HotelBooking.Application.Payments.Gateway;

public sealed record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    int PaymentId,
    int BookingId);
