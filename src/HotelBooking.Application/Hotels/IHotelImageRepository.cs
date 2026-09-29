using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Hotels;

public interface IHotelImageRepository
{
    void Add(HotelImage hotelImage);

    Task<HotelImage?> GetByIdAsync(
    int imageId,
    CancellationToken cancellationToken);

    void Remove(
    HotelImage hotelImage); 


    Task<int> GetNextDisplayOrderAsync(
    int hotelId,
    CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}