using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class HotelDetailsQuery : IHotelDetailsQuery
{
    private readonly ApplicationDbContext _dbContext;

    public HotelDetailsQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HotelDetails?> GetByIdAsync(
        GetHotelDetailsQuery query,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // 1. Get basic hotel information
        var hotel =
            await _dbContext.Hotels
                .AsNoTracking()
                .Where(hotel =>
                    hotel.Id == query.HotelId &&
                    !hotel.IsDeleted)
                .Select(hotel => new
                {
                    hotel.Id,
                    hotel.Name,
                    CityName = hotel.City.Name,
                    hotel.StarRating,
                    hotel.Category,
                    hotel.Description,
                    hotel.Address,
                    hotel.Latitude,
                    hotel.Longitude
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (hotel is null)
        {
            return null;
        }

        // 2. Get review summary
        var reviewSummary =
            await _dbContext.Reviews
                .AsNoTracking()
                .Where(review =>
                    review.Booking.HotelId ==
                    query.HotelId)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    AverageGuestRating =
                        (decimal?)group.Average(
                            review => review.Rating),

                    ReviewCount =
                        group.Count()
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

        // 3. Get most recent reviews
        var recentReviews =
            await _dbContext.Reviews
                .AsNoTracking()
                .Where(review =>
                    review.Booking.HotelId ==
                    query.HotelId)
                .OrderByDescending(review =>
                    review.CreatedAt)
                .ThenByDescending(review =>
                    review.Id)
                .Take(4)
                .Select(review =>
                    new HotelDetailsReview(
                        review.Rating,
                        review.Comment,
                        review.CreatedAt))
                .ToListAsync(cancellationToken);

        // 4. Find rooms unavailable during the requested date range
        var unavailableRoomIdsQuery =
            _dbContext.BookingRooms
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
                            bookingRoom.Booking.ExpiresAt >
                                now
                        )
                    ))
                .Select(bookingRoom =>
                    bookingRoom.RoomId);

        // 5. Get available physical rooms for this hotel
        var availableRooms =
            await _dbContext.Room
                .AsNoTracking()
                .Where(room =>
                    room.HotelId == query.HotelId &&
                    !unavailableRoomIdsQuery.Contains(
                        room.Id))
                .Select(room =>
                    new RoomCapacity(
                        room.Id,
                        room.RoomType,
                        room.AdultsCapacity,
                        room.ChildrenCapacity,
                        room.PricePerNight))
                .ToListAsync(cancellationToken);

        /*
         * A room type is returned only when the requested
         * number of rooms of that type can accommodate the
         * requested adults and children together.
         */
        var availableRoomTypes =
            availableRooms
                .GroupBy(room =>
                    room.RoomType)
                .Where(group =>
                    CanAccommodateGuests(
                        group,
                        query.Rooms,
                        query.Adults,
                        query.Children))
                .OrderBy(group =>
                    group.Key)
                .Select(group =>
                    new AvailableRoomSummary(
                        group.Key,
                        group.Count(),
                        group.Max(room =>
                            room.AdultsCapacity),
                        group.Max(room =>
                            room.ChildrenCapacity),
                        group.Min(room =>
                            room.PricePerNight)))
                .ToList();

        // 6. Get hotel gallery
        var images =
            await _dbContext.Set<HotelImage>()
                .AsNoTracking()
                .Where(image =>
                    image.HotelId ==
                    query.HotelId)
                .OrderByDescending(image =>
                    image.IsPrimary)
                .ThenBy(image =>
                    image.DisplayOrder)
                .ThenBy(image =>
                    image.Id)
                .Select(image =>
                    new HotelImageSummary(
                        image.Id,
                        image.StorageKey,
                        image.DisplayOrder,
                        image.IsPrimary))
                .ToListAsync(
                    cancellationToken);

        // 7. Return complete hotel details
        return new HotelDetails(
            hotel.Id,
            hotel.Name,
            hotel.CityName,
            hotel.StarRating,
            hotel.Category,
            hotel.Description,
            hotel.Address,
            hotel.Latitude,
            hotel.Longitude,
            reviewSummary?.AverageGuestRating,
            reviewSummary?.ReviewCount ?? 0,
            recentReviews,
            images,
            availableRoomTypes);
    }

    private static bool CanAccommodateGuests(
        IEnumerable<RoomCapacity> rooms,
        int requestedRooms,
        int requestedAdults,
        int requestedChildren)
    {
        var states =
            new HashSet<CapacityState>
            {
                new(
                    Rooms: 0,
                    Adults: 0,
                    Children: 0)
            };

        foreach (var room in rooms)
        {
            var currentStates =
                states.ToArray();

            foreach (var state in currentStates)
            {
                if (state.Rooms >= requestedRooms)
                {
                    continue;
                }

                var next =
                    new CapacityState(
                        Rooms:
                            state.Rooms + 1,

                        Adults:
                            Math.Min(
                                requestedAdults,
                                state.Adults +
                                room.AdultsCapacity),

                        Children:
                            Math.Min(
                                requestedChildren,
                                state.Children +
                                room.ChildrenCapacity));

                if (next.Rooms == requestedRooms &&
                    next.Adults >= requestedAdults &&
                    next.Children >= requestedChildren)
                {
                    return true;
                }

                states.Add(next);
            }
        }

        return false;
    }

    private sealed record RoomCapacity(
        int Id,
        RoomType RoomType,
        int AdultsCapacity,
        int ChildrenCapacity,
        decimal PricePerNight);

    private readonly record struct CapacityState(
        int Rooms,
        int Adults,
        int Children);
}