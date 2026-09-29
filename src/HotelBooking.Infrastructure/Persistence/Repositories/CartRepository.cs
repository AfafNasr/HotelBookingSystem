using HotelBooking.Application.Carts;
using HotelBooking.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CartRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Cart?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Carts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(
                cart => cart.UserId == userId,
                cancellationToken);
    }

    public void Add(Cart cart)
    {
        _dbContext.Carts.Add(cart);
    }

    public void Remove(Cart cart)
    {
        _dbContext.Carts.Remove(cart);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}