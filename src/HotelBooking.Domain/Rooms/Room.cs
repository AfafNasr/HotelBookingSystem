using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Rooms;

public sealed class Room
{
    public int Id { get; private set; }
    public int HotelId { get; private set; }

    public string RoomNumber { get; private set; } = null!;
    public RoomType RoomType { get; private set; }
    public string? Description { get; private set; }

    public int AdultsCapacity { get; private set; }
    public int ChildrenCapacity { get; private set; }
    public decimal PricePerNight { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    private Room()
    {
    }

    public Room(
        int hotelId,
        string roomNumber,
        RoomType roomType,
        string? description,
        int adultsCapacity,
        int childrenCapacity,
        decimal pricePerNight,
        DateTime createdAt)
    {
        HotelId = hotelId;
        RoomNumber = roomNumber.Trim();
        RoomType = roomType;
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        AdultsCapacity = adultsCapacity;
        ChildrenCapacity = childrenCapacity;
        PricePerNight = pricePerNight;
        CreatedAt = createdAt;
        IsDeleted = false;
    }

    public void Update(
    string roomNumber,
    RoomType roomType,
    string? description,
    int adultsCapacity,
    int childrenCapacity,
    decimal pricePerNight,
    DateTime updatedAt)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted room cannot be updated.");
        }

        RoomNumber = roomNumber.Trim();
        RoomType = roomType;

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        AdultsCapacity = adultsCapacity;
        ChildrenCapacity = childrenCapacity;
        PricePerNight = pricePerNight;
        UpdatedAt = updatedAt;
    }

    public void Delete(DateTime deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
    }


}