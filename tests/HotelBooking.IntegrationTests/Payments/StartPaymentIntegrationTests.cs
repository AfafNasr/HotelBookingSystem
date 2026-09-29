using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Payments.Gateway;
using HotelBooking.Application.Payments.StartPayment;
using HotelBooking.Domain.Bookings;
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

public sealed class StartPaymentIntegrationTests
{
    private const string Password = "Password123!";

    [Fact]
    public async Task StartPayment_WhenBookingIsValid_ShouldCreatePendingPaymentAndPaymentIntent()
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
                scenario,
                DateTime.UtcNow.AddMinutes(30));

        StartPaymentResult result;

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var handler =
                scope.ServiceProvider
                    .GetRequiredService<
                        StartPaymentCommandHandler>();

            result =
                await handler.HandleAsync(
                    new StartPaymentCommand(
                        bookingId),
                    CancellationToken.None);
        }

        Assert.True(result.Succeeded);
        Assert.NotNull(result.PaymentId);

        Assert.Equal(
            gateway.ClientSecret,
            result.ClientSecret);

        Assert.Equal(
            1,
            gateway.CreatePaymentIntentCallCount);

        Assert.Equal(
            0,
            gateway.GetPaymentIntentCallCount);

        Assert.NotNull(
            gateway.LastCreatePaymentIntentRequest);

        Assert.Equal(
            bookingId,
            gateway.LastCreatePaymentIntentRequest!
                .BookingId);

        Assert.Equal(
            scenario.PricePerNight * 3,
            gateway.LastCreatePaymentIntentRequest.Amount);

        Assert.Equal(
            "usd",
            gateway.LastCreatePaymentIntentRequest.Currency);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            verificationScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var payment =
            await dbContext.Payments
                .AsNoTracking()
                .SingleAsync(
                    payment =>
                        payment.BookingId == bookingId);

        Assert.Equal(
            result.PaymentId,
            payment.Id);

        Assert.Equal(
            gateway.ProviderPaymentIntentId,
            payment.ProviderPaymentIntentId);

        Assert.Equal(
            HotelBooking.Domain.Payments.PaymentStatus.Pending,
            payment.Status);

        Assert.Equal(
            "usd",
            payment.Currency);
    }

    [Fact]
    public async Task StartPayment_WhenCalledTwice_ShouldReuseExistingPaymentAndPaymentIntent()
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
                scenario,
                DateTime.UtcNow.AddMinutes(30));

        var firstResult =
            await ExecuteStartPaymentAsync(
                factory.Services,
                bookingId);

        var secondResult =
            await ExecuteStartPaymentAsync(
                factory.Services,
                bookingId);

        Assert.True(firstResult.Succeeded);
        Assert.True(secondResult.Succeeded);

        Assert.Equal(
            firstResult.PaymentId,
            secondResult.PaymentId);

        Assert.Equal(
            gateway.ClientSecret,
            secondResult.ClientSecret);

        Assert.Equal(
            1,
            gateway.CreatePaymentIntentCallCount);

        Assert.Equal(
            1,
            gateway.GetPaymentIntentCallCount);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var paymentCount =
            await dbContext.Payments
                .CountAsync(
                    payment =>
                        payment.BookingId == bookingId);

        Assert.Equal(
            1,
            paymentCount);
    }

    [Fact]
    public async Task StartPayment_WhenBookingHoldExpired_ShouldFailWithoutCreatingPayment()
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
                scenario,
                DateTime.UtcNow.AddMinutes(-1));

        var result =
            await ExecuteStartPaymentAsync(
                factory.Services,
                bookingId);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                BookingErrors.PaymentHoldExpired.Code);

        Assert.Equal(
            0,
            gateway.CreatePaymentIntentCallCount);

        Assert.Equal(
            0,
            gateway.GetPaymentIntentCallCount);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        Assert.False(
            await dbContext.Payments
                .AnyAsync(
                    payment =>
                        payment.BookingId == bookingId));
    }

    private static async Task<StartPaymentResult>
        ExecuteStartPaymentAsync(
            IServiceProvider services,
            int bookingId)
    {
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
            BookingScenario scenario,
            DateTime expiresAt)
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
                    $"payment-{unique}",

                Email =
                    $"payment-{unique}@test.com"
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
}