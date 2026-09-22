using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Domain.Bookings;
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
        var hotel = await _dbContext.Hotels
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
        var reviewSummary = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.Booking.HotelId == query.HotelId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                AverageGuestRating =
                    (decimal?)group.Average(review => review.Rating),

                ReviewCount = group.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        // 3. Get most recent reviews
        var recentReviews = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.Booking.HotelId == query.HotelId)
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Take(4)
            .Select(review => new HotelDetailsReview(
                review.Rating,
                review.Comment,
                review.CreatedAt))
            .ToListAsync(cancellationToken);

        // 4. Find rooms unavailable during the requested date range
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

        // 5. Get available physical rooms for this hotel
        var availableRoomsQuery = _dbContext.Room
            .AsNoTracking()
            .Where(room =>
                room.HotelId == query.HotelId &&
                !room.IsDeleted &&
                !unavailableRoomIdsQuery.Contains(room.Id));

        // 6. Group available physical rooms into room options
        var availableRoomTypes = await availableRoomsQuery
    .GroupBy(room => room.RoomType)
    .OrderBy(group => group.Key)
    .Select(group => new AvailableRoomSummary(
        group.Key,
        group.Count()))
    .ToListAsync(cancellationToken);

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
            availableRoomTypes);
    }
}