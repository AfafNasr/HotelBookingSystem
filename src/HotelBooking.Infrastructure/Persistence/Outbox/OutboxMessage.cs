namespace HotelBooking.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        string type,
        string deduplicationKey,
        string payload,
        DateTime occurredAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Outbox message id is required.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        Id = id;
        Type = type;
        DeduplicationKey = deduplicationKey;
        Payload = payload;
        OccurredAt = occurredAt;
        NextAttemptAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string DeduplicationKey { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime OccurredAt { get; private set; }

    public DateTime NextAttemptAt { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    public string? LastError { get; private set; }

    public void MarkProcessed(
        DateTime processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void RegisterFailure(
        DateTime failedAt,
        string error,
        int maxAttempts,
        TimeSpan retryDelay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        AttemptCount++;

        LastError =
            error.Length <= 2000
                ? error
                : error[..2000];

        if (AttemptCount >= maxAttempts)
        {
            FailedAt = failedAt;
            return;
        }

        NextAttemptAt =
            failedAt.Add(retryDelay);
    }
}