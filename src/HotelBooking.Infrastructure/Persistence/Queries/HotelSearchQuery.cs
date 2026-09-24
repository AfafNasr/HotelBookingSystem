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

        if (query.StarRating.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(
                hotel => hotel.StarRating == query.StarRating.Value);
        }

        if (query.Category.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(
                hotel => hotel.Category == query.Category.Value);
        }
        if (query.MinPrice.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(hotel =>
                availableRoomsQuery
                    .Where(room => room.HotelId == hotel.Id)
                    .Min(room => room.PricePerNight) >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(hotel =>
                availableRoomsQuery
                    .Where(room => room.HotelId == hotel.Id)
                    .Min(room => room.PricePerNight) <= query.MaxPrice.Value);
        }

        if (query.AmenityIds is { Count: > 0 })
        {
            var amenityIds = query.AmenityIds
                .Distinct()
                .ToArray();

            hotelsQuery = hotelsQuery.Where(hotel =>
                _dbContext.HotelAmenities
                    .Count(hotelAmenity =>
                        hotelAmenity.HotelId == hotel.Id &&
                        amenityIds.Contains(hotelAmenity.AmenityId))
                == amenityIds.Length);
        }

        var orderedHotelsQuery = query.SortBy switch
        {
            HotelSearchSort.PriceLowToHigh => hotelsQuery
                .OrderBy(hotel => availableRoomsQuery
                    .Where(room => room.HotelId == hotel.Id)
                    .Min(room => room.PricePerNight))
                .ThenBy(hotel => hotel.Id),

            HotelSearchSort.PriceHighToLow => hotelsQuery
                .OrderByDescending(hotel => availableRoomsQuery
                    .Where(room => room.HotelId == hotel.Id)
                    .Min(room => room.PricePerNight))
                .ThenBy(hotel => hotel.Id),

            HotelSearchSort.StarRatingHighToLow => hotelsQuery
                .OrderByDescending(hotel => hotel.StarRating)
                .ThenBy(hotel => hotel.Id),

            _ => hotelsQuery
                .OrderBy(hotel => hotel.Name)
                .ThenBy(hotel => hotel.Id)
        };

        var items = await orderedHotelsQuery
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