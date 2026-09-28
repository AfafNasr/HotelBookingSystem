namespace HotelBooking.Application.Common.Outbox;

public interface IOutboxWriter
{
    void EnqueueBookingConfirmation(
        int bookingId,
        DateTime occurredAt);
}