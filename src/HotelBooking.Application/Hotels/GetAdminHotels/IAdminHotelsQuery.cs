namespace HotelBooking.Application.Hotels.GetAdminHotels;

public interface IAdminHotelsQuery
{
    Task<IReadOnlyCollection<AdminHotel>> GetAsync(
        GetAdminHotelsQuery query,
        CancellationToken cancellationToken);
}

public sealed record AdminHotel(
    int Id,
    string Name,
    int StarRating,
    string Owner,
    int NumberOfRooms,
    DateTime CreatedAt,
    DateTime? UpdatedAt);