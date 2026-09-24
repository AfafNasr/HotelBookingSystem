using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Payments;

namespace HotelBooking.Domain.Bookings;

public sealed class Booking
{
    private readonly List<BookingRoom> _rooms = [];
    public IReadOnlyCollection<BookingRoom> Rooms => _rooms;
    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;
    public int HotelId { get; private set; }

    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }

    public BookingStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public string GuestFullName { get; private set; } = null!;
    public string GuestEmail { get; private set; } = null!;
    public string GuestPhoneNumber { get; private set; } = null!;
    public string? SpecialRequests { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string? ConfirmationNumber { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    public Payment? Payment { get; private set; }

    private Booking()
    {
    }

    public Booking(
        string userId,
        int hotelId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        DateTime expiresAt,
        string guestFullName,
        string guestEmail,
        string guestPhoneNumber,
        string? specialRequests,
        decimal totalAmount,
        DateTime createdAt)
    {
        UserId = userId;
        HotelId = hotelId;
        CheckInDate = checkInDate;
        CheckOutDate = checkOutDate;

        Status = BookingStatus.PendingPayment;
        ExpiresAt = expiresAt;

        GuestFullName = guestFullName.Trim();
        GuestEmail = guestEmail.Trim();
        GuestPhoneNumber = guestPhoneNumber.Trim();

        SpecialRequests = string.IsNullOrWhiteSpace(specialRequests)
            ? null
            : specialRequests.Trim();

        TotalAmount = totalAmount;
        CreatedAt = createdAt;
    }

    public void AddRoom(
        int roomId,
        decimal originalPricePerNight)
    {
        if (_rooms.Any(bookingRoom => bookingRoom.RoomId == roomId))
        {
            throw new InvalidOperationException(
                "The same room cannot be added to a booking more than once.");
        }

        _rooms.Add(new BookingRoom(
            roomId,
            originalPricePerNight));
    }

    public void Expire(DateTime expiredAt)
    {
        if (Status != BookingStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only a pending payment booking can expire.");
        }

        if (ExpiresAt is null || ExpiresAt > expiredAt)
        {
            throw new InvalidOperationException(
                "The booking payment hold has not expired yet.");
        }

        Status = BookingStatus.Expired;
        UpdatedAt = expiredAt;
    }

    public void Confirm(
    string confirmationNumber,
    DateTime confirmedAt)
    {
        if (Status == BookingStatus.Confirmed)
        {
            return;
        }

        if (Status != BookingStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only a pending payment booking can be confirmed.");
        }

        if (ExpiresAt is null || ExpiresAt <= confirmedAt)
        {
            throw new InvalidOperationException(
                "An expired booking payment hold cannot be confirmed.");
        }

        if (string.IsNullOrWhiteSpace(confirmationNumber))
        {
            throw new ArgumentException(
                "Confirmation number is required.",
                nameof(confirmationNumber));
        }

        ConfirmationNumber = confirmationNumber.Trim();
        Status = BookingStatus.Confirmed;
        ExpiresAt = null;
        UpdatedAt = confirmedAt;
    }
}