using HotelBooking.Application.Bookings.Expiration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.BackgroundJobs;

public sealed class BookingExpirationWorker : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationWorker> _logger;

    public BookingExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirePendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while expiring pending bookings.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task ExpirePendingBookingsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var expirationService =
            scope.ServiceProvider
                .GetRequiredService<ExpirePendingBookingsService>();

        var expiredCount = await expirationService.ExecuteAsync(
            DateTime.UtcNow,
            cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "Expired {ExpiredBookingCount} pending bookings.",
                expiredCount);
        }
    }
}