using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Application.Payments.HandleStripeWebhook;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Payments;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.IntegrationTests.TestData;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotelBooking.IntegrationTests.Payments;

public sealed class HandleStripeWebhookIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task HandleWebhook_WhenPaymentSucceedsDuringActiveHold_ShouldConfirmBooking()
    {
        await using var baseFactory =
            new CustomWebApplicationFactory();

        var gateway =
            new FakePaymentGateway();

        var emailSender =
            new FakeEmailSender();

        var currentUser =
            new FakeCurrentUserService();

        await using var factory =
            CreateFactory(
                baseFactory,
                gateway,
                emailSender,
                currentUser);

        var owner =
            await CreateUserAsync(
                factory.Services);

        var customer =
            await CreateUserAsync(
                factory.Services);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreatePendingPaymentSetupAsync(
                factory.Services,
                customer.Id,
                scenario,
                DateTime.UtcNow.AddMinutes(30),
                gateway.ProviderPaymentIntentId);

        var result =
            await ExecuteWebhookAsync(
                factory.Services,
                new HandleStripeWebhookCommand(
                    "evt_success_1",
                    "payment_intent.succeeded",
                    gateway.ProviderPaymentIntentId));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var booking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    booking =>
                        booking.Id == setup.BookingId);

        var payment =
            await dbContext.Payments
                .AsNoTracking()
                .SingleAsync(
                    payment =>
                        payment.Id == setup.PaymentId);

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Equal(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                booking.ConfirmationNumber));

        Assert.StartsWith(
            "HB-",
            booking.ConfirmationNumber);

        Assert.Null(
            booking.ExpiresAt);

        Assert.Single(
            emailSender.SentMessages);

        var email =
            emailSender.SentMessages.Single();

        Assert.Equal(
            "payment.guest@test.com",
            email.To);

        Assert.Contains(
            booking.ConfirmationNumber!,
            email.Subject);

        Assert.Equal(
            0,
            gateway.CreateRefundCallCount);
    }

    [Fact]
    public async Task HandleWebhook_WhenSameSucceededEventIsProcessedAgain_ShouldRemainIdempotent()
    {
        await using var baseFactory =
            new CustomWebApplicationFactory();

        var gateway =
            new FakePaymentGateway();

        var emailSender =
            new FakeEmailSender();

        var currentUser =
            new FakeCurrentUserService();

        await using var factory =
            CreateFactory(
                baseFactory,
                gateway,
                emailSender,
                currentUser);

        var owner =
            await CreateUserAsync(
                factory.Services);

        var customer =
            await CreateUserAsync(
                factory.Services);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreatePendingPaymentSetupAsync(
                factory.Services,
                customer.Id,
                scenario,
                DateTime.UtcNow.AddMinutes(30),
                gateway.ProviderPaymentIntentId);

        var firstResult =
            await ExecuteWebhookAsync(
                factory.Services,
                new HandleStripeWebhookCommand(
                    "evt_success_1",
                    "payment_intent.succeeded",
                    gateway.ProviderPaymentIntentId));

        Assert.True(firstResult.Succeeded);

        string firstConfirmationNumber;

        await using (var firstScope =
            factory.Services.CreateAsyncScope())
        {
            var dbContext =
                firstScope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            firstConfirmationNumber =
                (await dbContext.Bookings
                    .AsNoTracking()
                    .SingleAsync(
                        booking =>
                            booking.Id == setup.BookingId))
                .ConfirmationNumber!;
        }

        var secondResult =
            await ExecuteWebhookAsync(
                factory.Services,
                new HandleStripeWebhookCommand(
                    "evt_success_1",
                    "payment_intent.succeeded",
                    gateway.ProviderPaymentIntentId));

        Assert.True(secondResult.Succeeded);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationDbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var bookingAfterSecondEvent =
            await verificationDbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    booking =>
                        booking.Id == setup.BookingId);

        var paymentAfterSecondEvent =
            await verificationDbContext.Payments
                .AsNoTracking()
                .SingleAsync(
                    payment =>
                        payment.Id == setup.PaymentId);

        Assert.Equal(
            BookingStatus.Confirmed,
            bookingAfterSecondEvent.Status);

        Assert.Equal(
            PaymentStatus.Succeeded,
            paymentAfterSecondEvent.Status);

        Assert.Equal(
            firstConfirmationNumber,
            bookingAfterSecondEvent.ConfirmationNumber);

        Assert.Single(
            emailSender.SentMessages);

        Assert.Equal(
            0,
            gateway.CreateRefundCallCount);
    }

    [Fact]
    public async Task HandleWebhook_WhenPaymentSucceedsAfterHoldExpired_ShouldRefundPaymentAndNotConfirmBooking()
    {
        await using var baseFactory =
            new CustomWebApplicationFactory();

        var gateway =
            new FakePaymentGateway();

        var emailSender =
            new FakeEmailSender();

        var currentUser =
            new FakeCurrentUserService();

        await using var factory =
            CreateFactory(
                baseFactory,
                gateway,
                emailSender,
                currentUser);

        var owner =
            await CreateUserAsync(
                factory.Services);

        var customer =
            await CreateUserAsync(
                factory.Services);

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreatePendingPaymentSetupAsync(
                factory.Services,
                customer.Id,
                scenario,
                DateTime.UtcNow.AddMinutes(-1),
                gateway.ProviderPaymentIntentId);

        var result =
            await ExecuteWebhookAsync(
                factory.Services,
                new HandleStripeWebhookCommand(
                    "evt_late_payment",
                    "payment_intent.succeeded",
                    gateway.ProviderPaymentIntentId));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var booking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    booking =>
                        booking.Id == setup.BookingId);

        var payment =
            await dbContext.Payments
                .AsNoTracking()
                .SingleAsync(
                    payment =>
                        payment.Id == setup.PaymentId);

        var refund =
            await dbContext.Refunds
                .AsNoTracking()
                .SingleAsync(
                    refund =>
                        refund.PaymentId ==
                        setup.PaymentId);

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.NotEqual(
            BookingStatus.Confirmed,
            booking.Status);

        Assert.Null(
            booking.ConfirmationNumber);

        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);

        Assert.Equal(
            gateway.ProviderRefundId,
            refund.ProviderRefundId);

        Assert.Equal(
            payment.Amount,
            refund.Amount);

        Assert.Equal(
            payment.Currency,
            refund.Currency);

        Assert.Equal(
            1,
            gateway.CreateRefundCallCount);

        Assert.NotNull(
            gateway.LastCreateRefundRequest);

        Assert.Equal(
            setup.PaymentId,
            gateway.LastCreateRefundRequest!
                .RefundId is > 0
                    ? refund.PaymentId
                    : -1);

        Assert.Equal(
            gateway.ProviderPaymentIntentId,
            gateway.LastCreateRefundRequest
                .ProviderPaymentIntentId);

        Assert.Equal(
            payment.Amount,
            gateway.LastCreateRefundRequest.Amount);

        Assert.Empty(
            emailSender.SentMessages);
    }

    private static async Task<HandleStripeWebhookResult>
        ExecuteWebhookAsync(
            IServiceProvider services,
            HandleStripeWebhookCommand command)
    {
        await using var scope =
            services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    HandleStripeWebhookCommandHandler>();

        return await handler.HandleAsync(
            command,
            CancellationToken.None);
    }

    private static async Task<PaymentSetup>
        CreatePendingPaymentSetupAsync(
            IServiceProvider services,
            string userId,
            BookingScenario scenario,
            DateTime expiresAt,
            string providerPaymentIntentId)
    {
        await using var scope =
            services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var now =
            DateTime.UtcNow;

        var checkInDate =
            DateOnly.FromDateTime(
                now.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var totalAmount =
            scenario.PricePerNight * 3;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                expiresAt,
                "Payment Test Guest",
                "payment.guest@test.com",
                "+970599000000",
                null,
                totalAmount,
                now);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        var payment =
            new Payment(
                booking.Id,
                totalAmount,
                "usd",
                now);

        payment.AttachProviderPaymentIntent(
            providerPaymentIntentId,
            now);

        dbContext.Payments.Add(
            payment);

        await dbContext.SaveChangesAsync();

        return new PaymentSetup(
            booking.Id,
            payment.Id);
    }

    private static async Task<IdentityUser>
        CreateUserAsync(
            IServiceProvider services)
    {
        await using var scope =
            services.CreateAsyncScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<IdentityUser>>();

        var unique =
            Guid.NewGuid().ToString("N");

        var user =
            new IdentityUser
            {
                UserName =
                    $"webhook-{unique}",

                Email =
                    $"webhook-{unique}@test.com"
            };

        var result =
            await userManager.CreateAsync(
                user,
                Password);

        Assert.True(
            result.Succeeded,
            string.Join(
                ", ",
                result.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    private static WebApplicationFactory<Program>
        CreateFactory(
            CustomWebApplicationFactory baseFactory,
            FakePaymentGateway gateway,
            FakeEmailSender emailSender,
            FakeCurrentUserService currentUser)
    {
        return baseFactory.WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureTestServices(
                    services =>
                    {
                        services.RemoveAll<
                            IPaymentGateway>();

                        services.AddSingleton<
                            IPaymentGateway>(
                            gateway);

                        services.RemoveAll<
                            IEmailSender>();

                        services.AddSingleton<
                            IEmailSender>(
                            emailSender);

                        services.RemoveAll<
                            ICurrentUserService>();

                        services.AddSingleton<
                            ICurrentUserService>(
                            currentUser);
                    });
            });
    }

    private sealed record PaymentSetup(
        int BookingId,
        int PaymentId);
}