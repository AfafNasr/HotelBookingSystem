namespace HotelBooking.Domain.Amenities;

public sealed class Amenity
{
    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Amenity()
    {
    }

    public Amenity(
        string name,
        DateTime createdAt)
    {
        Name = name.Trim();
        CreatedAt = createdAt;
        IsDeleted = false;
    }
}