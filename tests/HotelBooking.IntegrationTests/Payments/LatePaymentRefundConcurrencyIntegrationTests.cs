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

public sealed class LatePaymentRefundConcurrencyIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task DuplicateLatePaymentWebhooks_ShouldCreateSingleRefund()
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
            new ConcurrentRefundPaymentGateway();

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
            await CreateExpiredPaymentAsync(
                factory.Services,
                customer.Id,
                scenario,
                fixedNow.UtcDateTime,
                gateway.ProviderPaymentIntentId);

        /*
         * Both webhook handlers are released at approximately
         * the same time.
         *
         * Each handler gets its own DI scope / DbContext.
         *
         * Production code is responsible for serializing work
         * on the same booking using IBookingConcurrencyManager.
         */
        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask =
            ExecuteWebhookAfterGateAsync(
                factory.Services,
                "evt_late_payment_1",
                gateway.ProviderPaymentIntentId,
                startGate.Task);

        var secondTask =
            ExecuteWebhookAfterGateAsync(
                factory.Services,
                "evt_late_payment_2",
                gateway.ProviderPaymentIntentId,
                startGate.Task);

        startGate.SetResult();

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        /*
         * Duplicate Stripe delivery is expected behavior.
         *
         * Both requests should therefore complete successfully
         * rather than exposing a database concurrency exception.
         */
        Assert.All(
            results,
            result =>
                Assert.True(
                    result.Succeeded,
                    string.Join(
                        ", ",
                        result.Errors.Select(
                            error => error.Code))));

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

        /*
         * A late payment must never resurrect an expired booking.
         */
        Assert.Equal(
            BookingStatus.Expired,
            booking.Status);

        Assert.Null(
            booking.ConfirmationNumber);

        /*
         * Stripe reported that the payment succeeded,
         * so our local Payment must reflect that state.
         */
        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        /*
         * Database invariant:
         * one payment may have only one refund.
         */
        Assert.Single(
            refunds);

        var refund =
            refunds.Single();

        Assert.Equal(
            RefundStatus.Succeeded,
            refund.Status);

        Assert.Equal(
     $"{gateway.ProviderRefundId}_{refund.Id}",
     refund.ProviderRefundId);

        Assert.Equal(
            payment.Amount,
            refund.Amount);

        Assert.Equal(
            payment.Currency,
            refund.Currency);

        /*
         * External financial invariant:
         *
         * Duplicate webhook delivery must not cause two calls
         * to the payment provider to refund the same payment.
         */
        Assert.Equal(
     1,
     gateway.CreatedRefundCount);

        /*
         * Late payment is refunded, not confirmed,
         * so no booking confirmation email should be sent.
         */
        Assert.Empty(
            emailSender.SentMessages);
    }

    private static async Task<HandleStripeWebhookResult>
        ExecuteWebhookAfterGateAsync(
            IServiceProvider services,
            string eventId,
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
                eventId,
                "payment_intent.succeeded",
                providerPaymentIntentId),
            CancellationToken.None);
    }

    private static async Task<PaymentSetup>
        CreateExpiredPaymentAsync(
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
         * Persist an already-expired booking so this test remains
         * focused specifically on duplicate refund processing.
         */
        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(-1),
                "Duplicate Refund Guest",
                "duplicate.refund@test.com",
                "+970599000000",
                null,
                totalAmount,
                now.AddMinutes(-30));

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        booking.Expire(
            now);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        /*
         * The payment already has a Stripe PaymentIntent attached,
         * because the webhook is reporting success for that intent.
         *
         * Its local state remains Pending until the webhook handler
         * processes payment_intent.succeeded.
         */
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
                    $"refund-concurrency-{unique}",

                Email =
                    $"refund-concurrency-{unique}@test.com"
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
            ConcurrentRefundPaymentGateway gateway,
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

                        /*
                         * Intentionally do NOT replace IRefundRepository.
                         *
                         * This test now exercises:
                         *
                         * real RefundRepository
                         * real ApplicationDbContext
                         * real SQL Server
                         * real unique constraints
                         * real booking locking
                         *
                         * Only external systems are faked.
                         */
                    });
            });
    }

    private sealed class ConcurrentRefundPaymentGateway
     : IPaymentGateway
    {
        private readonly object _sync = new();

        private readonly Dictionary<int, CreateRefundResult>
            _refundsByRefundId = [];

        private int _createRefundCallCount;

        public string ProviderPaymentIntentId { get; } =
            "pi_duplicate_late_payment";

        public string ProviderRefundId { get; } =
            "re_duplicate_late_payment";

        public int CreateRefundCallCount =>
            Volatile.Read(
                ref _createRefundCallCount);

        public int CreatedRefundCount
        {
            get
            {
                lock (_sync)
                {
                    return _refundsByRefundId.Count;
                }
            }
        }

        public Task<CreatePaymentIntentResult>
            CreatePaymentIntentAsync(
                CreatePaymentIntentRequest request,
                CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Payment intent creation is not expected in this test.");
        }

        public Task<CreatePaymentIntentResult>
            GetPaymentIntentAsync(
                string providerPaymentIntentId,
                CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Payment intent retrieval is not expected in this test.");
        }

        public Task<CreateRefundResult>
            CreateRefundAsync(
                CreateRefundRequest request,
                CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _createRefundCallCount);

            lock (_sync)
            {
                if (_refundsByRefundId.TryGetValue(
                        request.RefundId,
                        out var existing))
                {
                    return Task.FromResult(existing);
                }

                var created =
                    new CreateRefundResult(
                        $"{ProviderRefundId}_{request.RefundId}");

                _refundsByRefundId.Add(
                    request.RefundId,
                    created);

                return Task.FromResult(created);
            }
        }
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