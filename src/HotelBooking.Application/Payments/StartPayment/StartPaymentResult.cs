using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Payments.StartPayment;

public sealed record StartPaymentResult(
    bool Succeeded,
    int? PaymentId,
    string? ClientSecret,
    IReadOnlyCollection<ApplicationError> Errors);