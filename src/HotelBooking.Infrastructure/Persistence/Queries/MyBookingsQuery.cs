using HotelBooking.Application.Bookings.GetMyBookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class MyBookingsQuery : IMyBookingsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public MyBookingsQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MyBooking>> GetAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => new MyBooking(
                booking.Id,
                booking.Hotel.Name,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.TotalAmount,
                booking.Status,
                booking.ConfirmationNumber,
                booking.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}