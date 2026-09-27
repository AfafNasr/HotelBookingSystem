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
        var hotelsQuery = _dbContext.Hotels
            .AsNoTracking()
            .Where(hotel =>
                !hotel.IsDeleted &&
                (
                    hotel.Name.Contains(query.Destination) ||
                    hotel.City.Name.Contains(query.Destination)
                ));

        if (query.StarRating.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(
                hotel =>
                    hotel.StarRating ==
                    query.StarRating.Value);
        }

        if (query.Category.HasValue)
        {
            hotelsQuery = hotelsQuery.Where(
                hotel =>
                    hotel.Category ==
                    query.Category.Value);
        }

        if (query.AmenityIds is { Count: > 0 })
        {
            var amenityIds = query.AmenityIds
                .Distinct()
                .ToArray();

            hotelsQuery = hotelsQuery.Where(
                hotel =>
                    _dbContext.HotelAmenities
                        .Count(hotelAmenity =>
                            hotelAmenity.HotelId == hotel.Id &&
                            amenityIds.Contains(
                                hotelAmenity.AmenityId))
                    == amenityIds.Length);
        }

        var unavailableRoomIdsQuery = _dbContext.BookingRooms
            .AsNoTracking()
            .Where(bookingRoom =>
                bookingRoom.Booking.CheckInDate <
                    query.CheckOutDate &&
                bookingRoom.Booking.CheckOutDate >
                    query.CheckInDate &&
                (
                    bookingRoom.Booking.Status ==
                        BookingStatus.Confirmed ||
                    (
                        bookingRoom.Booking.Status ==
                            BookingStatus.PendingPayment &&
                        bookingRoom.Booking.ExpiresAt > now
                    )
                ))
            .Select(bookingRoom =>
                bookingRoom.RoomId);

        var availableRoomsQuery = _dbContext.Room
            .AsNoTracking()
            .Where(room =>
                !room.IsDeleted &&
                !unavailableRoomIdsQuery.Contains(
                    room.Id));

        /*
         * Important performance change:
         *
         * Previously Count, Sum, Sum and Min were calculated
         * as separate correlated subqueries for every hotel.
         *
         * Now available rooms are grouped once by HotelId and
         * all required statistics are calculated together.
         */
        var availableRoomStatsQuery = availableRoomsQuery
            .GroupBy(room => room.HotelId)
            .Select(group => new
            {
                HotelId = group.Key,

                RoomCount =
                    group.Count(),

                AdultsCapacity =
                    group.Sum(
                        room =>
                            room.AdultsCapacity),

                ChildrenCapacity =
                    group.Sum(
                        room =>
                            room.ChildrenCapacity),

                MinPrice =
                    group.Min(
                        room =>
                            room.PricePerNight)
            });

        var hotelsWithAvailabilityQuery =
            from hotel in hotelsQuery
            join stats in availableRoomStatsQuery
                on hotel.Id equals stats.HotelId
            where
                stats.RoomCount >= query.Rooms &&
                stats.AdultsCapacity >= query.Adults &&
                stats.ChildrenCapacity >= query.Children
            select new
            {
                Hotel = hotel,
                Stats = stats
            };

        if (query.MinPrice.HasValue)
        {
            hotelsWithAvailabilityQuery =
                hotelsWithAvailabilityQuery.Where(
                    item =>
                        item.Stats.MinPrice >=
                        query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            hotelsWithAvailabilityQuery =
                hotelsWithAvailabilityQuery.Where(
                    item =>
                        item.Stats.MinPrice <=
                        query.MaxPrice.Value);
        }

        var orderedHotelsQuery =
            query.SortBy switch
            {
                HotelSearchSort.PriceLowToHigh =>
                    hotelsWithAvailabilityQuery
                        .OrderBy(item =>
                            item.Stats.MinPrice)
                        .ThenBy(item =>
                            item.Hotel.Id),

                HotelSearchSort.PriceHighToLow =>
                    hotelsWithAvailabilityQuery
                        .OrderByDescending(item =>
                            item.Stats.MinPrice)
                        .ThenBy(item =>
                            item.Hotel.Id),

                HotelSearchSort.StarRatingHighToLow =>
                    hotelsWithAvailabilityQuery
                        .OrderByDescending(item =>
                            item.Hotel.StarRating)
                        .ThenBy(item =>
                            item.Hotel.Id),

                _ =>
                    hotelsWithAvailabilityQuery
                        .OrderBy(item =>
                            item.Hotel.Name)
                        .ThenBy(item =>
                            item.Hotel.Id)
            };

        var items = await orderedHotelsQuery
            .Skip(
                (query.Page - 1) *
                query.PageSize)
            .Take(
                query.PageSize + 1)
            .Select(item =>
                new SearchHotelItem(
                    item.Hotel.Id,
                    item.Hotel.Name,
                    item.Hotel.City.Name,
                    item.Hotel.StarRating,
                    item.Hotel.Description,
                    item.Stats.MinPrice,

                    _dbContext.HotelImages
                        .Where(image =>
                            image.HotelId ==
                            item.Hotel.Id)
                        .OrderByDescending(image =>
                            image.IsPrimary)
                        .ThenBy(image =>
                            image.DisplayOrder)
                        .Select(image =>
                            image.StorageKey)
                        .FirstOrDefault()))
            .ToListAsync(
                cancellationToken);

        var hasNextPage =
            items.Count > query.PageSize;

        if (hasNextPage)
        {
            items.RemoveAt(
                items.Count - 1);
        }

        return new SearchHotelsPage(
            items,
            hasNextPage);
    }
}