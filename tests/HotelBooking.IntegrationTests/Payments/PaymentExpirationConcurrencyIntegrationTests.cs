using HotelBooking.Application.Bookings.Expiration;
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

public sealed class PaymentExpirationConcurrencyIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task WebhookAndExpiration_WhenHoldAlreadyExpired_ShouldEndWithExpiredBookingAndRefundedPayment()
    {
        var fixedNow =
            new DateTimeOffset(
                2026,
                9,
                26,
                18,
                0,
                0,
                TimeSpan.Zero);

        var timeProvider =
            new FixedTimeProvider(
                fixedNow);

        var gateway =
            new FakePaymentGateway
            {
                ProviderPaymentIntentId =
                    "pi_expiration_race",

                ProviderRefundId =
                    "re_expiration_race"
            };

        var emailSender =
            new FakeEmailSender();

        await using var baseFactory =
            new CustomWebApplicationFactory();

        await using var factory =
            CreateFactory(
                baseFactory,
                gateway,
                emailSender,
                timeProvider);

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
            await CreateExpiredPendingPaymentAsync(
                factory.Services,
                customer.Id,
                scenario,
                fixedNow.UtcDateTime,
                gateway.ProviderPaymentIntentId);

        /*
         * Both operations wait on the same gate.
         *
         * Once released, each operation runs in its own DI scope /
         * DbContext and competes for the same booking-level SQL lock.
         */
        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var expirationTask =
            ExecuteExpirationAfterGateAsync(
                factory.Services,
                fixedNow.UtcDateTime,
                startGate.Task);

        var webhookTask =
            ExecuteWebhookAfterGateAsync(
                factory.Services,
                gateway.ProviderPaymentIntentId,
                startGate.Task);

        startGate.SetResult();

        var expirationResult =
            await expirationTask;

        var webhookResult =
            await webhookTask;

        Assert.True(
            webhookResult.Succeeded,
            string.Join(
                ", ",
                webhookResult.Errors.Select(
                    error => error.Code)));

        /*
         * Expiration may return 0 or 1 depending on which operation
         * reaches its initial query first.
         *
         * What matters is the final persisted state, not which
         * process "won" the race.
         */
        Assert.InRange(
            expirationResult,
            0,
            1);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
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

        var refunds =
            await dbContext.Refunds
                .AsNoTracking()
                .Where(
                    refund =>
                        refund.PaymentId ==
                        setup.PaymentId)
                .ToListAsync();

        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.Null(
            booking.ConfirmationNumber);

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        Assert.Single(
            refunds);

        var refund =
            refunds.Single();

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

        Assert.Empty(
            emailSender.SentMessages);
    }

    private static async Task<int>
        ExecuteExpirationAfterGateAsync(
            IServiceProvider services,
            DateTime now,
            Task startGate)
    {
        await startGate;

        await using var scope =
            services.CreateAsyncScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<
                    ExpirePendingBookingsService>();

        return await service.ExecuteAsync(
            now,
            CancellationToken.None);
    }

    private static async Task<
        HandleStripeWebhookResult>
        ExecuteWebhookAfterGateAsync(
            IServiceProvider services,
            string providerPaymentIntentId,
            Task startGate)
    {
        await startGate;

        await using var scope =
            services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    HandleStripeWebhookCommandHandler>();

        return await handler.HandleAsync(
            new HandleStripeWebhookCommand(
                "evt_expiration_race",
                "payment_intent.succeeded",
                providerPaymentIntentId),
            CancellationToken.None);
    }

    private static async Task<PaymentSetup>
        CreateExpiredPendingPaymentAsync(
            IServiceProvider services,
            string userId,
            BookingScenario scenario,
            DateTime now,
            string providerPaymentIntentId)
    {
        await using var scope =
            services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var checkInDate =
            DateOnly.FromDateTime(
                now.AddDays(10));

        var checkOutDate =
            checkInDate.AddDays(3);

        var nights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var totalAmount =
            scenario.PricePerNight * nights;

        /*
         * The hold is already expired relative to the shared
         * fixed time used by both competing operations.
         */
        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(-1),
                "Expiration Race Guest",
                "expiration.race@test.com",
                "+970599000000",
                null,
                totalAmount,
                now.AddMinutes(-20));

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
                now.AddMinutes(-10));

        payment.AttachProviderPaymentIntent(
            providerPaymentIntentId,
            now.AddMinutes(-10));

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
                    $"expiration-race-{unique}",

                Email =
                    $"expiration-race-{unique}@test.com"
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
            TimeProvider timeProvider)
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
                            TimeProvider>();

                        services.AddSingleton(
                            timeProvider);
                    });
            });
    }

    private sealed record PaymentSetup(
        int BookingId,
        int PaymentId);

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow.ToUniversalTime();
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}