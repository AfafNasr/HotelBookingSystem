using System.Text.Json;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Emails;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessor
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;

    private readonly ApplicationDbContext _dbContext;
    private readonly IBookingConfirmationQuery _bookingConfirmationQuery;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeProvider _timeProvider;

    public OutboxProcessor(
        ApplicationDbContext dbContext,
        IBookingConfirmationQuery bookingConfirmationQuery,
        IEmailSender emailSender,
        ILogger<OutboxProcessor> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _bookingConfirmationQuery = bookingConfirmationQuery;
        _emailSender = emailSender;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var messages =
            await _dbContext.OutboxMessages
                .Where(message =>
                    message.ProcessedAt == null &&
                    message.FailedAt == null &&
                    message.NextAttemptAt <= now &&
                    message.AttemptCount < MaxAttempts)
                .OrderBy(message =>
                    message.OccurredAt)
                .ThenBy(message =>
                    message.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(
                message,
                cancellationToken);
        }

        return messages.Count;
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (message.Type)
            {
                case OutboxMessageTypes.BookingConfirmationEmail:
                    await ProcessBookingConfirmationAsync(
                        message,
                        cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported outbox message type '{message.Type}'.");
            }

            message.MarkProcessed(
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedAt =
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime;

            var nextAttemptNumber =
                message.AttemptCount + 1;

            message.RegisterFailure(
                failedAt,
                exception.Message,
                MaxAttempts,
                CalculateRetryDelay(
                    nextAttemptNumber));

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            if (message.FailedAt is not null)
            {
                _logger.LogError(
                    exception,
                    "Outbox message {OutboxMessageId} reached the retry limit. Type: {OutboxMessageType}.",
                    message.Id,
                    message.Type);

                return;
            }

            _logger.LogWarning(
                exception,
                "Outbox message {OutboxMessageId} failed and will be retried. Attempt: {AttemptCount}.",
                message.Id,
                message.AttemptCount);
        }
    }

    private async Task ProcessBookingConfirmationAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payload =
            JsonSerializer.Deserialize<
                BookingConfirmationOutboxPayload>(
                message.Payload)
            ?? throw new InvalidOperationException(
                $"Outbox message {message.Id} contains an invalid payload.");

        var confirmation =
            await _bookingConfirmationQuery.GetAsync(
                payload.BookingId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Booking confirmation could not be loaded for booking {payload.BookingId}.");

        var email =
            BookingConfirmationEmailBuilder.Build(
                confirmation);

        await _emailSender.SendAsync(
            email,
            cancellationToken);
    }

    private static TimeSpan CalculateRetryDelay(
        int attemptNumber)
    {
        /*
         * 1 → 30 seconds
         * 2 → 1 minute
         * 3 → 2 minutes
         * 4 → 4 minutes
         * 5 → 8 minutes
         */
        var exponent =
            Math.Max(
                0,
                attemptNumber - 1);

        var seconds =
            30 * Math.Pow(
                2,
                exponent);

        return TimeSpan.FromSeconds(
            seconds);
    }
}