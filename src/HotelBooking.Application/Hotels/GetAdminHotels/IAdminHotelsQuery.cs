namespace HotelBooking.Application.Hotels.GetAdminHotels;

public interface IAdminHotelsQuery
{
    Task<IReadOnlyCollection<AdminHotel>> GetAsync(
        GetAdminHotelsQuery query,
        CancellationToken cancellationToken);
}