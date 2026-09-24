namespace HotelBooking.Application.Payments.Gateway;

public interface IPaymentGateway
{
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken);

    Task<CreatePaymentIntentResult> GetPaymentIntentAsync(
    string providerPaymentIntentId,
    CancellationToken cancellationToken);

    Task<CreateRefundResult> CreateRefundAsync(
    CreateRefundRequest request,
    CancellationToken cancellationToken);
}