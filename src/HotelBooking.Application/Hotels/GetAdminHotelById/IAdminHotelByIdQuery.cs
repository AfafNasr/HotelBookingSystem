using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels.GetAdminHotelById;

public interface IAdminHotelByIdQuery
{
    Task<AdminHotelDetails?> GetByIdAsync(
        int hotelId,
        CancellationToken cancellationToken);
}

public sealed record AdminHotelDetails(
    int Id,
    string Name,
    int CityId,
    string CityName,
    string OwnerId,
    string OwnerName,
    int StarRating,
    HotelCategory Category,
    string? Description,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    DateTime CreatedAt,
    DateTime? UpdatedAt);