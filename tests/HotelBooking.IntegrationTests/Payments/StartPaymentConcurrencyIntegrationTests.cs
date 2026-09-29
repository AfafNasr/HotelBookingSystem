using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Application.Payments.StartPayment;
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

public sealed class StartPaymentConcurrencyIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task StartPayment_WhenTwoRequestsRunConcurrently_ShouldReuseSinglePayment()
    {
        await using var baseFactory =
            new CustomWebApplicationFactory();

        var gateway =
            new ConcurrentPaymentGateway();

        var currentUser =
            new FakeCurrentUserService();

        await using var factory =
            CreateFactory(
                baseFactory,
                gateway,
                currentUser);

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

        var bookingId =
            await CreatePendingBookingAsync(
                factory.Services,
                customer.Id,
                scenario);

        /*
         * Start both operations at approximately the same time.
         *
         * We do NOT synchronize inside the repository anymore,
         * because StartPayment now holds a booking-level lock.
         */
        var startGate =
            new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask =
            ExecuteStartPaymentAfterGateAsync(
                factory.Services,
                bookingId,
                startGate.Task);

        var secondTask =
            ExecuteStartPaymentAfterGateAsync(
                factory.Services,
                bookingId,
                startGate.Task);

        startGate.SetResult();

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        Assert.All(
            results,
            result =>
                Assert.True(
                    result.Succeeded,
                    string.Join(
                        ", ",
                        result.Errors.Select(
                            error => error.Code))));

        var paymentIds =
            results
                .Select(result => result.PaymentId)
                .Distinct()
                .ToArray();

        Assert.Single(paymentIds);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var payments =
            await dbContext.Payments
                .AsNoTracking()
                .Where(
                    payment =>
                        payment.BookingId == bookingId)
                .ToListAsync();

        Assert.Single(payments);

        var payment =
            payments.Single();

        Assert.Equal(
            PaymentStatus.Pending,
            payment.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                payment.ProviderPaymentIntentId));

        /*
         * Critical invariant:
         * only one external payment intent should be created.
         */
        Assert.Equal(
    1,
    gateway.CreatedPaymentIntentCount);

        /*
         * The second request should reuse the existing intent.
         */

    }

    private static async Task<StartPaymentResult>
        ExecuteStartPaymentAfterGateAsync(
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
                    StartPaymentCommandHandler>();

        return await handler.HandleAsync(
            new StartPaymentCommand(
                bookingId),
            CancellationToken.None);
    }

    private static async Task<int>
        CreatePendingBookingAsync(
            IServiceProvider services,
            string userId,
            BookingScenario scenario)
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

        var nights =
            checkOutDate.DayNumber -
            checkInDate.DayNumber;

        var booking =
            new Booking(
                userId,
                scenario.HotelId,
                checkInDate,
                checkOutDate,
                now.AddMinutes(30),
                "Concurrent Payment Guest",
                "payment.concurrent@test.com",
                "+970599000000",
                null,
                scenario.PricePerNight * nights,
                now);

        booking.AddRoom(
            scenario.RoomId,
            scenario.PricePerNight);

        dbContext.Bookings.Add(
            booking);

        await dbContext.SaveChangesAsync();

        return booking.Id;
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
                    $"payment-concurrency-{unique}",

                Email =
                    $"payment-concurrency-{unique}@test.com"
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
            ConcurrentPaymentGateway gateway,
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
                            ICurrentUserService>();

                        services.AddSingleton<
                            ICurrentUserService>(
                            currentUser);
                    });
            });
    }

    private sealed class ConcurrentPaymentGateway
    : IPaymentGateway
    {
        private readonly object _sync = new();

        private readonly Dictionary<int, CreatePaymentIntentResult>
            _paymentIntentsByPaymentId = [];

        private int _createPaymentIntentCallCount;
        private int _getPaymentIntentCallCount;

        public int CreatePaymentIntentCallCount =>
            Volatile.Read(
                ref _createPaymentIntentCallCount);

        public int GetPaymentIntentCallCount =>
            Volatile.Read(
                ref _getPaymentIntentCallCount);

        public int CreatedPaymentIntentCount
        {
            get
            {
                lock (_sync)
                {
                    return _paymentIntentsByPaymentId.Count;
                }
            }
        }

        public Task<CreatePaymentIntentResult>
            CreatePaymentIntentAsync(
                CreatePaymentIntentRequest request,
                CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _createPaymentIntentCallCount);

            lock (_sync)
            {
                if (_paymentIntentsByPaymentId.TryGetValue(
                        request.PaymentId,
                        out var existing))
                {
                    return Task.FromResult(existing);
                }

                var created =
                    new CreatePaymentIntentResult(
                        $"pi_payment_{request.PaymentId}",
                        $"secret_payment_{request.PaymentId}");

                _paymentIntentsByPaymentId.Add(
                    request.PaymentId,
                    created);

                return Task.FromResult(created);
            }
        }

        public Task<CreatePaymentIntentResult>
            GetPaymentIntentAsync(
                string providerPaymentIntentId,
                CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _getPaymentIntentCallCount);

            lock (_sync)
            {
                var existing =
                    _paymentIntentsByPaymentId
                        .Values
                        .SingleOrDefault(
                            intent =>
                                intent.ProviderPaymentIntentId ==
                                providerPaymentIntentId);

                if (existing is null)
                {
                    throw new InvalidOperationException(
                        $"PaymentIntent '{providerPaymentIntentId}' does not exist.");
                }

                return Task.FromResult(existing);
            }
        }

        public Task<CreateRefundResult>
            CreateRefundAsync(
                CreateRefundRequest request,
                CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Refund creation is not expected in this test.");
        }
    }

}