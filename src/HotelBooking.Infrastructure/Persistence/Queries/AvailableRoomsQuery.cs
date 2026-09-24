using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class AvailableRoomsQuery : IAvailableRoomsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public AvailableRoomsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AvailableRoom>> GetAsync(
        GetAvailableRoomsQuery query,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var unavailableRoomIdsQuery = _dbContext.BookingRooms
            .AsNoTracking()
            .Where(bookingRoom =>
                bookingRoom.Booking.CheckInDate < query.CheckOutDate &&
                bookingRoom.Booking.CheckOutDate > query.CheckInDate &&
                (
                    bookingRoom.Booking.Status == BookingStatus.Confirmed ||
                    (
                        bookingRoom.Booking.Status == BookingStatus.PendingPayment &&
                        bookingRoom.Booking.ExpiresAt > now
                    )
                ))
            .Select(bookingRoom => bookingRoom.RoomId);

        var rooms = await _dbContext.Room
            .AsNoTracking()
            .Where(room =>
                !room.IsDeleted &&
                room.HotelId == query.HotelId &&
                room.RoomType == query.RoomType &&
                room.AdultsCapacity >= query.Adults &&
                room.ChildrenCapacity >= query.Children &&
                !unavailableRoomIdsQuery.Contains(room.Id))
            .OrderBy(room => room.PricePerNight)
            .ThenBy(room => room.Id)
            .Select(room => new AvailableRoom(
                room.Id,
                room.RoomType,
                room.Description,
                room.AdultsCapacity,
                room.ChildrenCapacity,
                room.PricePerNight,
                _dbContext.RoomImages
                    .Where(image => image.RoomId == room.Id)
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .ThenBy(image => image.Id)
                    .Select(image => new AvailableRoomImage(
                        image.StorageKey,
                        image.DisplayOrder,
                        image.IsPrimary))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return rooms;
    }
}