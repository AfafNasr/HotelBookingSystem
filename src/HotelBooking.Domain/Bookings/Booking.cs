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
}