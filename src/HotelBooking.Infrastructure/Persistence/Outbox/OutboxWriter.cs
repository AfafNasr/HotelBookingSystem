using System.Text.Json;
using HotelBooking.Application.Common.Outbox;

namespace HotelBooking.Infrastructure.Persistence.Outbox;

public sealed class OutboxWriter
    : IOutboxWriter
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxWriter(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void EnqueueBookingConfirmation(
        int bookingId,
        DateTime occurredAt)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingId));
        }

        var payload =
            JsonSerializer.Serialize(
                new BookingConfirmationOutboxPayload(
                    bookingId));

        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                OutboxMessageTypes
                    .BookingConfirmationEmail,
                $"booking-confirmation:{bookingId}",
                payload,
                occurredAt);

        _dbContext.OutboxMessages.Add(
            message);
    }
}