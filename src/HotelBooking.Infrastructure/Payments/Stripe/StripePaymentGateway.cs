using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Payments;
using Microsoft.Extensions.Options;
using Stripe;

namespace HotelBooking.Infrastructure.Payments.Stripe;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeClient _stripeClient;

    public StripePaymentGateway(
        IOptions<StripeOptions> options)
    {
        _stripeClient = new StripeClient(
            options.Value.SecretKey);
    }

    public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        var amountInMinorUnits = ConvertToMinorUnits(
            request.Amount);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInMinorUnits,
            Currency = request.Currency,

            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            },

            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = request.PaymentId.ToString(),
                ["booking_id"] = request.BookingId.ToString()
            }
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"payment-{request.PaymentId}"
        };

        var paymentIntent =
            await _stripeClient.V1.PaymentIntents.CreateAsync(
                options,
                requestOptions,
                cancellationToken);

        return new CreatePaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.ClientSecret);
    }

    public async Task<CreatePaymentIntentResult> GetPaymentIntentAsync(
    string providerPaymentIntentId,
    CancellationToken cancellationToken)
    {
        var paymentIntent =
            await _stripeClient.V1.PaymentIntents.GetAsync(
                providerPaymentIntentId,
                cancellationToken: cancellationToken);

        return new CreatePaymentIntentResult(
            paymentIntent.Id,
            paymentIntent.ClientSecret);
    }


    public async Task<CreateRefundResult> CreateRefundAsync(
    CreateRefundRequest request,
    CancellationToken cancellationToken)
    {
        var amountInMinorUnits = ConvertToMinorUnits(
            request.Amount);

        var options = new RefundCreateOptions
        {
            PaymentIntent = request.ProviderPaymentIntentId,
            Amount = amountInMinorUnits
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"refund-{request.RefundId}"
        };

        var refund =
            await _stripeClient.V1.Refunds.CreateAsync(
                options,
                requestOptions,
                cancellationToken);

        return new CreateRefundResult(
            refund.Id);
    }

    private static long ConvertToMinorUnits(decimal amount)
    {
        return checked(
            (long)decimal.Round(
                amount * 100,
                0,
                MidpointRounding.AwayFromZero));
    }

}