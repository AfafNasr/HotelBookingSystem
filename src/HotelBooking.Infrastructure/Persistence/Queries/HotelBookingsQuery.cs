using HotelBooking.Application.Bookings.GetHotelBookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class HotelBookingsQuery : IHotelBookingsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public HotelBookingsQuery(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HotelBookingsPage> GetAsync(
        int hotelId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.HotelId == hotelId)
            .OrderByDescending(booking =>
                booking.CreatedAt)
            .ThenByDescending(booking =>
                booking.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(booking =>
                new HotelBookingItem(
                    booking.Id,
                    booking.GuestFullName,
                    booking.GuestEmail,
                    booking.GuestPhoneNumber,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.Rooms.Count,
                    booking.TotalAmount,
                    booking.Status,
                    booking.ConfirmationNumber,
                    booking.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage =
            bookings.Count > pageSize;

        if (hasNextPage)
        {
            bookings.RemoveAt(
                bookings.Count - 1);
        }

        return new HotelBookingsPage(
            bookings,
            hasNextPage);
    }
}