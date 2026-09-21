using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Common.Interfaces;

public interface IRoomImageRepository
{
    void Add(RoomImage roomImage);

    Task<int> GetNextDisplayOrderAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
