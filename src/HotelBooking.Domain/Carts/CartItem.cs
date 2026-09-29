using HotelBooking.Domain.Rooms;

namespace HotelBooking.Domain.Carts;

public sealed class CartItem
{
    public int CartId { get; private set; }

    public int RoomId { get; private set; }

    public DateTime AddedAt { get; private set; }

    public Cart Cart { get; private set; } = null!;

    public Room Room { get; private set; } = null!;

    private CartItem()
    {
    }

    internal CartItem(
        int roomId,
        DateTime addedAt)
    {
        RoomId = roomId;
        AddedAt = addedAt;
    }
}