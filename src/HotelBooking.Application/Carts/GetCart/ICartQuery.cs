namespace HotelBooking.Application.Carts.GetCart;

public interface ICartQuery
{
    Task<CartDetails?> GetAsync(
        string userId,
        CancellationToken cancellationToken);
}

public sealed record CartDetails(
    int CartId,
    int HotelId,
    string HotelName,
    IReadOnlyCollection<CartItemDetails> Items);