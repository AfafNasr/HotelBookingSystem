namespace HotelBooking.Infrastructure.Persistence.Outbox;

public sealed record BookingConfirmationOutboxPayload(
    int BookingId);