using HotelBooking.Application.Rooms.GetAdminRooms;
using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class AdminRoomsQuery : IAdminRoomsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public AdminRoomsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdminRoom>> GetAsync(
        GetAdminRoomsQuery request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(now);

        var query = _dbContext.Room
            .AsNoTracking()
            .Where(room => !room.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(room =>
                room.RoomNumber.Contains(search));
        }

        var rooms = await query
            .OrderBy(room => room.RoomNumber)
            .Select(room => new AdminRoom(
                room.Id,
                room.RoomNumber,

                !_dbContext.BookingRooms.Any(bookingRoom =>
                    bookingRoom.RoomId == room.Id &&
                    bookingRoom.Booking.CheckInDate <= today &&
                    bookingRoom.Booking.CheckOutDate > today &&
                    (
                        bookingRoom.Booking.Status == BookingStatus.Confirmed ||
                        (
                            bookingRoom.Booking.Status == BookingStatus.PendingPayment &&
                            bookingRoom.Booking.ExpiresAt > now
                        )
                    )),

                room.AdultsCapacity,
                room.ChildrenCapacity,
                room.CreatedAt,
                room.UpdatedAt))
            .ToListAsync(cancellationToken);

        return rooms;
    }
}