namespace HotelBooking.Application.Common.Payments;

public sealed record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    int PaymentId,
    int BookingId);
