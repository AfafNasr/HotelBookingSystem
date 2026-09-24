using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Payments.StartPayment;

public sealed record StartPaymentResult(
    bool Succeeded,
    int? PaymentId,
    string? ClientSecret,
    IReadOnlyCollection<ApplicationError> Errors);