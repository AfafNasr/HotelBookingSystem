namespace HotelBooking.Application.Hotels.GetHotelDetails;

public interface IHotelDetailsQuery
{
    Task<HotelDetails?> GetByIdAsync(
        GetHotelDetailsQuery query,
        DateTime now,
        CancellationToken cancellationToken);
}