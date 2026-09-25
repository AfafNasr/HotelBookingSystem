namespace HotelBooking.Application.Hotels.GetAdminHotelById;

public interface IAdminHotelByIdQuery
{
    Task<AdminHotelDetails?> GetByIdAsync(
        int hotelId,
        CancellationToken cancellationToken);
}