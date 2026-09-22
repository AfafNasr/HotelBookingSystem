using HotelBooking.Domain.Bookings;

namespace HotelBooking.Domain.Reviews;

public sealed class Review
{
    public int Id { get; private set; }

    public int BookingId { get; private set; }

    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Booking Booking { get; private set; } = null!;

    private Review()
    {
    }

    public Review(
        int bookingId,
        int rating,
        string? comment,
        DateTime createdAt)
    {
        BookingId = bookingId;
        Rating = rating;

        Comment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();

        CreatedAt = createdAt;
    }

    public void Update(
        int rating,
        string? comment,
        DateTime updatedAt)
    {
        Rating = rating;

        Comment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();

        UpdatedAt = updatedAt;
    }
}