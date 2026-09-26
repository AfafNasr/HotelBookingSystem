using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Payments.Gateway;

namespace HotelBooking.IntegrationTests.Payments;

internal sealed class FakeCurrentUserService
    : ICurrentUserService
{
    public string? UserId { get; set; }

    public bool IsInRole(string role)
    {
        return false;
    }
}

internal sealed class FakePaymentGateway
    : IPaymentGateway
{
    public int CreatePaymentIntentCallCount { get; private set; }

    public int GetPaymentIntentCallCount { get; private set; }

    public int CreateRefundCallCount { get; private set; }

    public CreatePaymentIntentRequest? LastCreatePaymentIntentRequest
    {
        get;
        private set;
    }

    public CreateRefundRequest? LastCreateRefundRequest
    {
        get;
        private set;
    }

    public string ProviderPaymentIntentId { get; set; }
        = "pi_integration_test";

    public string ClientSecret { get; set; }
        = "secret_integration_test";

    public string ProviderRefundId { get; set; }
        = "re_integration_test";

    public Task<CreatePaymentIntentResult>
        CreatePaymentIntentAsync(
            CreatePaymentIntentRequest request,
            CancellationToken cancellationToken)
    {
        CreatePaymentIntentCallCount++;
        LastCreatePaymentIntentRequest = request;

        return Task.FromResult(
            new CreatePaymentIntentResult(
                ProviderPaymentIntentId,
                ClientSecret));
    }

    public Task<CreatePaymentIntentResult>
        GetPaymentIntentAsync(
            string providerPaymentIntentId,
            CancellationToken cancellationToken)
    {
        GetPaymentIntentCallCount++;

        return Task.FromResult(
            new CreatePaymentIntentResult(
                providerPaymentIntentId,
                ClientSecret));
    }

    public Task<CreateRefundResult>
        CreateRefundAsync(
            CreateRefundRequest request,
            CancellationToken cancellationToken)
    {
        CreateRefundCallCount++;
        LastCreateRefundRequest = request;

        return Task.FromResult(
            new CreateRefundResult(
                ProviderRefundId));
    }
}

internal sealed class FakeEmailSender
    : IEmailSender
{
    public List<EmailMessage> SentMessages { get; } = [];

    public Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        SentMessages.Add(message);

        return Task.CompletedTask;
    }
}