namespace HotelBooking.Application.Common.Errors;

public static class PaymentErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Payment.NotFound",
            "The payment was not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError NotPending =
        new(
            "Payment.NotPending",
            "The payment is no longer pending.",
            ErrorType.Conflict);

    public static readonly ApplicationError ProviderPaymentIntentMissing =
        new(
            "Payment.ProviderPaymentIntentMissing",
            "The payment provider intent ID is missing.",
            ErrorType.Conflict);
}