using HotelBooking.Domain.Hotels;

namespace HotelBooking.Application.Common.Interfaces;

public interface IHotelRepository
{
    void Add(Hotel hotel);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}