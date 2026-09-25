using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Hotels;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class HotelRepository : IHotelRepository
{
    private readonly ApplicationDbContext _dbContext;

    public HotelRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Hotel?> GetByIdAsync(
    int hotelId,
    CancellationToken cancellationToken)
    {
        return _dbContext.Hotels
            .SingleOrDefaultAsync(
                hotel =>
                    hotel.Id == hotelId &&
                    !hotel.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasActiveOrUpcomingBookingsAsync(
    int hotelId,
    DateOnly today,
    DateTime now,
    CancellationToken cancellationToken)
    {
        return _dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                booking =>
                    booking.HotelId == hotelId &&
                    booking.CheckOutDate > today &&
                    (
                        booking.Status == BookingStatus.Confirmed ||
                        (
                            booking.Status == BookingStatus.PendingPayment &&
                            booking.ExpiresAt > now
                        )
                    ),
                cancellationToken);
    }

    public void Add(Hotel hotel)
    {
        _dbContext.Hotels.Add(hotel);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}