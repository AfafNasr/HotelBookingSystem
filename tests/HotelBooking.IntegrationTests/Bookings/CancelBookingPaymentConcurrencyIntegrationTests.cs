using HotelBooking.Application.Bookings.CancelBooking;
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

namespace HotelBooking.IntegrationTests.Bookings;

public sealed class CancelBookingPaymentConcurrencyIntegrationTests
{
    [Fact]
    public async Task CancelBooking_AndPaymentSucceededWebhook_WhenConcurrent_ShouldEndInConsistentState()
    {
        // Arrange
        await using var baseFactory =
            new CustomWebApplicationFactory();

        var now =
            new DateTimeOffset(
                2026,
                9,
                27,
                10,
                0,
                0,
                TimeSpan.Zero);

        var paymentGateway =
            new FakePaymentGateway();

        var emailSender =
            new FakeEmailSender();

        var currentUser =
            new FakeCurrentUserService();

        var timeProvider =
            new TestTimeProvider(now);

        await using var factory =
            CreateFactory(
                baseFactory,
                paymentGateway,
                emailSender,
                currentUser,
                timeProvider);

        var owner =
            await CreateUserAsync(
                factory.Services);

        var customer =
            await CreateUserAsync(
                factory.Services);

        currentUser.UserId =
            customer.Id;

        var scenario =
            await BookingTestData.CreateAsync(
                factory.Services,
                owner);

        var setup =
            await CreatePendingBookingWithPaymentAsync(
                factory.Services,
                customer.Id,
                scenario,
                now.UtcDateTime);

        /*
         * Both operations will wait on this gate.
         *
         * Releasing it makes cancellation and the webhook begin as
         * close together as possible while still using separate scopes
         * and separate DbContext instances.
         */
        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var cancelTask =
            ExecuteCancelAsync(
                factory.Services,
                setup.BookingId,
                startGate.Task);

        var webhookTask =
            ExecuteWebhookAsync(
                factory.Services,
                setup.ProviderPaymentIntentId,
                startGate.Task);

        // Act
        startGate.SetResult();

        var cancelResult =
            await cancelTask;

        var webhookResult =
            await webhookTask;

        // Assert
        Assert.True(
            webhookResult.Succeeded);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var booking =
            await dbContext.Bookings
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.Id == setup.BookingId);

        var payment =
            await dbContext.Payments
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.BookingId == setup.BookingId);

        var refunds =
            await dbContext.Refunds
                .AsNoTracking()
                .Where(
                    refund =>
                        refund.PaymentId == payment.Id)
                .ToListAsync();

        Assert.Equal(
            PaymentStatus.Succeeded,
            payment.Status);

        /*
         * There are exactly two valid serializations.
         *
         * CASE A - Cancellation wins the booking lock:
         *
         * PendingPayment
         *      ↓
         * Cancelled
         *      ↓
         * webhook arrives
         *      ↓
         * Payment Succeeded + Refund Succeeded
         *
         *
         * CASE B - Webhook wins the booking lock:
         *
         * PendingPayment
         *      ↓
         * Confirmed
         *      ↓
         * cancellation enters afterward
         *      ↓
         * Booking.CannotCancel
         *
         * What must NEVER happen is an inconsistent mixture such as:
         *
         * Cancelled + confirmation number
         * Confirmed + refund
         * Cancelled + successful payment without refund
         */
        if (booking.Status == BookingStatus.Cancelled)
        {
            // Cancellation won.
            Assert.True(
                cancelResult.Succeeded);

            Assert.Null(
                booking.ConfirmationNumber);

            Assert.Null(
                booking.ExpiresAt);

            var refund =
                Assert.Single(refunds);

            Assert.Equal(
                RefundStatus.Succeeded,
                refund.Status);

            Assert.Equal(
                payment.Amount,
                refund.Amount);

            Assert.Equal(
                payment.Currency,
                refund.Currency);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    refund.ProviderRefundId));

            Assert.Equal(
                1,
                paymentGateway.CreateRefundCallCount);

            Assert.Equal(
                0,
                emailSender.SendCallCount);
        }
        else if (booking.Status == BookingStatus.Confirmed)
        {
            // Webhook won.
            Assert.False(
                cancelResult.Succeeded);

            Assert.Contains(
                cancelResult.Errors,
                error =>
                    error.Code ==
                    "Booking.CannotCancel");

            Assert.NotNull(
                booking.ConfirmationNumber);

            Assert.Null(
                booking.ExpiresAt);

            Assert.Empty(
                refunds);

            Assert.Equal(
                0,
                paymentGateway.CreateRefundCallCount);

            Assert.Equal(
                1,
                emailSender.SendCallCount);
        }
        else
        {
            Assert.Fail(
                $"Unexpected final booking status: " +
                $"{booking.Status}.");
        }
    }

    // ============================================================
    // CONCURRENT OPERATIONS
    // ============================================================

    private static async Task<CancelBookingResult>
        ExecuteCancelAsync(
            IServiceProvider services,
            int bookingId,
            Task startGate)
    {
        await startGate;

        await using var scope =
            services.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<
                    CancelBookingCommandHandler>();

        return await handler.HandleAsync(
            new CancelBookingCommand(
                bookingId),
            CancellationToken.None);
    }

    private static async Task<HandleStripeWebhookResult>
        ExecuteWebhookAsync(
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
                $"evt_{Guid.NewGuid():N}",
                "payment_intent.succeeded",
                providerPaymentIntentId),
            CancellationToken.None);
    }

    // ============================================================
    // TEST DATA
    // ============================================================

    private static async Task<PaymentSetup>
        CreatePendingBookingWithPaymentAsync(
            IServiceProvider services,
            string userId,
            BookingScenario scenario,
            DateTime now)
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

        var totalAmount =
            scenario.PricePerNight * 3;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Concurrency Customer",
                "concurrency.customer@test.com",
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

        var providerPaymentIntentId =
            $"pi_{Guid.NewGuid():N}";

        var payment =
            new Payment(
                booking.Id,
                booking.TotalAmount,
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
            payment.Id,
            providerPaymentIntentId);
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
            Guid.NewGuid()
                .ToString("N");

        var user =
            new IdentityUser
            {
                UserName =
                    $"concurrency-{unique}",

                Email =
                    $"concurrency-{unique}@test.com"
            };

        var result =
            await userManager.CreateAsync(
                user,
                "Password123!");

        Assert.True(
            result.Succeeded,
            string.Join(
                ", ",
                result.Errors.Select(
                    error =>
                        error.Description)));

        return user;
    }

    // ============================================================
    // TEST HOST
    // ============================================================

    private static WebApplicationFactory<Program>
        CreateFactory(
            CustomWebApplicationFactory baseFactory,
            FakePaymentGateway paymentGateway,
            FakeEmailSender emailSender,
            FakeCurrentUserService currentUser,
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
                            paymentGateway);

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

                        services.RemoveAll<
                            TimeProvider>();

                        services.AddSingleton(
                            timeProvider);
                    });
            });
    }

    // ============================================================
    // TEST DOUBLES
    // ============================================================

    private sealed class FakeCurrentUserService
        : ICurrentUserService
    {
        public string? UserId { get; set; }

        public bool IsInRole(
            string role)
        {
            return false;
        }
    }

    private sealed class FakePaymentGateway
        : IPaymentGateway
    {
        private int _createRefundCallCount;

        public int CreateRefundCallCount =>
            _createRefundCallCount;

        public Task<CreatePaymentIntentResult>
            CreatePaymentIntentAsync(
                CreatePaymentIntentRequest request,
                CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new CreatePaymentIntentResult(
                    "pi_unused",
                    "secret_unused"));
        }

        public Task<CreatePaymentIntentResult>
            GetPaymentIntentAsync(
                string providerPaymentIntentId,
                CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new CreatePaymentIntentResult(
                    providerPaymentIntentId,
                    "secret_unused"));
        }

        public Task<CreateRefundResult>
            CreateRefundAsync(
                CreateRefundRequest request,
                CancellationToken cancellationToken)
        {
            var callNumber =
                Interlocked.Increment(
                    ref _createRefundCallCount);

            return Task.FromResult(
                new CreateRefundResult(
                    $"re_cancel_concurrency_{callNumber}"));
        }
    }

    private sealed class FakeEmailSender
        : IEmailSender
    {
        private int _sendCallCount;

        public int SendCallCount =>
            _sendCallCount;

        public Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _sendCallCount);

            return Task.CompletedTask;
        }
    }

    private sealed class TestTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed record PaymentSetup(
        int BookingId,
        int PaymentId,
        string ProviderPaymentIntentId);
}