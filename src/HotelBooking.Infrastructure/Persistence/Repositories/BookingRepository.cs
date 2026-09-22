using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BookingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<int>> GetUnavailableRoomIdsAsync(
        IReadOnlyCollection<int> roomIds,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BookingRooms
            .AsNoTracking()
            .Where(bookingRoom =>
                roomIds.Contains(bookingRoom.RoomId) &&

                // Stay dates use an exclusive checkout boundary.
                // A booking ending on the requested check-in date does not overlap.
                bookingRoom.Booking.CheckInDate < checkOutDate &&
                bookingRoom.Booking.CheckOutDate > checkInDate &&

                // Confirmed bookings always block the room.
                // Pending bookings block it only while their temporary hold is active.
                (bookingRoom.Booking.Status == BookingStatus.Confirmed ||
                 (bookingRoom.Booking.Status == BookingStatus.PendingPayment &&
                  bookingRoom.Booking.ExpiresAt > now)))
            .Select(bookingRoom => bookingRoom.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>>
        GetExpiredPendingBookingIdsAsync(
            DateTime now,
            CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.Status == BookingStatus.PendingPayment &&
                booking.ExpiresAt <= now)
            .Select(booking => booking.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Booking?> GetByIdAsync(
    int bookingId,
    CancellationToken cancellationToken)
    {
        return _dbContext.Bookings
            .SingleOrDefaultAsync(
                booking => booking.Id == bookingId,
                cancellationToken);
    }

    public void Add(Booking booking)
    {
        _dbContext.Bookings.Add(booking);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}