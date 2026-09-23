using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Queries;

public sealed class BookingConfirmationQuery : IBookingConfirmationQuery
{
    private readonly ApplicationDbContext _dbContext;

    public BookingConfirmationQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BookingConfirmation?> GetAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.Id == bookingId)
            .Select(booking => new BookingConfirmation(
                booking.UserId,
                booking.Status,
                booking.ConfirmationNumber,
                booking.Hotel.Name,
                booking.Hotel.Address,
                booking.GuestFullName,
                booking.GuestEmail,
                booking.CheckInDate,
                booking.CheckOutDate,

                booking.Rooms
                    .Select(bookingRoom => new BookingConfirmationRoom(
                        bookingRoom.RoomId,
                        bookingRoom.Room.RoomType,
                        bookingRoom.Room.Description,
                        bookingRoom.OriginalPricePerNight))
                    .ToList(),

                booking.TotalAmount,

                booking.Payment == null
                    ? null
                    : (PaymentStatus?)booking.Payment.Status,

                booking.Payment == null
                    ? null
                    : (decimal?)booking.Payment.Amount,

                booking.Payment == null
                    ? null
                    : booking.Payment.Currency))
            .SingleOrDefaultAsync(cancellationToken);
    }
}