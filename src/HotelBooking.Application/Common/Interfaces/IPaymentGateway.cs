using HotelBooking.Application.Common.Payments;

namespace HotelBooking.Application.Common.Interfaces;

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