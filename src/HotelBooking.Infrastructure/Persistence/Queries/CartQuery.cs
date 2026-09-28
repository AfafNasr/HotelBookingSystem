using HotelBooking.Application.Carts.GetCart;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class CartQuery : ICartQuery
{
    private readonly ApplicationDbContext _dbContext;

    public CartQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CartDetails?> GetAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Carts
            .AsNoTracking()
            .Where(cart => cart.UserId == userId)
            .Select(cart => new CartDetails(
                cart.Id,
                cart.HotelId,
                cart.Hotel.Name,
                cart.Items
                    .OrderBy(item => item.AddedAt)
                    .Select(item => new CartItemDetails(
                        item.RoomId,
                        item.Room.RoomNumber,
                        item.Room.RoomType,
                        item.Room.Description,
                        item.Room.AdultsCapacity,
                        item.Room.ChildrenCapacity,
                        item.Room.PricePerNight,
                        !item.Room.IsDeleted,
                        item.AddedAt))
                    .ToArray()))
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}