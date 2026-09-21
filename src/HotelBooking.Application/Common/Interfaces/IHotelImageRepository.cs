using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Common.Interfaces;

public interface IHotelImageRepository
{
    void Add(HotelImage hotelImage);
    Task<int> GetNextDisplayOrderAsync(
    int hotelId,
    CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}