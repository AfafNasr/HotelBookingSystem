namespace HotelBooking.Domain.Rooms;

public sealed class RoomImage
{
    public int Id { get; private set; }

    public int RoomId { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public int DisplayOrder { get; private set; }

    public bool IsPrimary { get; private set; }

    public Room Room { get; private set; } = null!;

    private RoomImage()
    {
    }

    public RoomImage(
        int roomId,
        string storageKey,
        int displayOrder,
        bool isPrimary)
    {
        RoomId = roomId;
        StorageKey = storageKey;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public void ReplaceStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException(
                "Storage key is required.",
                nameof(storageKey));
        }

        StorageKey = storageKey.Trim();
    }
}