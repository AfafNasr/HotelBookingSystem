namespace HotelBooking.Application.Payments.StartPayment;

public sealed record StartPaymentCommand(
    int BookingId);