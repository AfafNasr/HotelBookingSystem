namespace HotelBooking.Domain.Hotels;

public sealed class HotelImage
{
    public int Id { get; private set; }

    public int HotelId { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    private HotelImage()
    {
    }

    public HotelImage(
        int hotelId,
        string storageKey,
        int displayOrder,
        bool isPrimary)
    {
        HotelId = hotelId;
        StorageKey = storageKey;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }
}