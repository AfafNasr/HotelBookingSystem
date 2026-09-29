using HotelBooking.Domain.Hotels;

namespace HotelBooking.Domain.Carts;

public sealed class Cart
{
    private readonly List<CartItem> _items = [];

    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public int HotelId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Hotel Hotel { get; private set; } = null!;

    public IReadOnlyCollection<CartItem> Items => _items;

    private Cart()
    {
    }

    public Cart(
        string userId,
        int hotelId,
        DateTime createdAt)
    {
        UserId = userId;
        HotelId = hotelId;
        CreatedAt = createdAt;
    }

    public void AddRoom(
        int roomId,
        DateTime addedAt)
    {
        if (_items.Any(item => item.RoomId == roomId))
        {
            return;
        }

        _items.Add(
            new CartItem(
                roomId,
                addedAt));

        UpdatedAt = addedAt;
    }

    public bool RemoveRoom(
        int roomId,
        DateTime updatedAt)
    {
        var item =
            _items.SingleOrDefault(
                item => item.RoomId == roomId);

        if (item is null)
        {
            return false;
        }

        _items.Remove(item);

        UpdatedAt = updatedAt;

        return true;
    }

    public bool ContainsRoom(int roomId)
    {
        return _items.Any(
            item => item.RoomId == roomId);
    }

    public bool IsEmpty()
    {
        return _items.Count == 0;
    }
}