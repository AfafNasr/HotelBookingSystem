namespace HotelBooking.Application.Bookings.GetHotelBookings;

public sealed record GetHotelBookingsQuery(
    int HotelId,
    int Page = 1,
    int PageSize = 20);