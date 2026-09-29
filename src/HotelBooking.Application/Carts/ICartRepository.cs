using HotelBooking.Domain.Carts;

namespace HotelBooking.Application.Carts;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken);

    void Add(Cart cart);

    void Remove(Cart cart);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}