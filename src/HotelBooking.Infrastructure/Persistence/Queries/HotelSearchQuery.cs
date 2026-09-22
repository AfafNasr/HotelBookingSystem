using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class HotelSearchQuery : IHotelSearchQuery
{
    private readonly ApplicationDbContext _dbContext;

    public HotelSearchQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchHotelsPage> SearchAsync(
        SearchHotelsQuery query,
        DateTime now,
        CancellationToken cancellationToken)
    {
        //this query checks for hotels that are not deleted
        //and whose name or city name contains
        //the destination specified in the query.
        var hotelsQuery = _dbContext.Hotels
          .AsNoTracking()
          .Where(hotel =>
               !hotel.IsDeleted &&
               (hotel.Name.Contains(query.Destination) ||
               hotel.City.Name.Contains(query.Destination)));


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

        var availableRoomsQuery = _dbContext.Room
          .AsNoTracking()
          .Where(room =>
              !room.IsDeleted &&
              !unavailableRoomIdsQuery.Contains(room.Id));

        hotelsQuery = hotelsQuery
           .Where(hotel =>
        availableRoomsQuery.Count(room => room.HotelId == hotel.Id) >= query.Rooms &&
        availableRoomsQuery
            .Where(room => room.HotelId == hotel.Id)
            .Sum(room => room.AdultsCapacity) >= query.Adults &&
        availableRoomsQuery
            .Where(room => room.HotelId == hotel.Id)
            .Sum(room => room.ChildrenCapacity) >= query.Children);


        var items = await hotelsQuery
    .OrderBy(hotel => hotel.Name)
    .ThenBy(hotel => hotel.Id)
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize + 1)
    .Select(hotel => new SearchHotelItem(
        hotel.Id,
        hotel.Name,
        hotel.City.Name,
        hotel.StarRating,
        hotel.Description,
        availableRoomsQuery
            .Where(room => room.HotelId == hotel.Id)
            .Min(room => room.PricePerNight),
        _dbContext.HotelImages
            .Where(image => image.HotelId == hotel.Id)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .Select(image => image.StorageKey)
            .FirstOrDefault()))
    .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > query.PageSize;

        if (hasNextPage)
        {
            items.RemoveAt(items.Count - 1);
        }



        return new SearchHotelsPage( items, hasNextPage);

    }
}