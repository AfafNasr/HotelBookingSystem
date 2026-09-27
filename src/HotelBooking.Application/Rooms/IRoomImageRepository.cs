using HotelBooking.Domain.Rooms;

namespace HotelBooking.Application.Rooms;

public interface IRoomImageRepository
{
    void Add(RoomImage roomImage);

    Task<RoomImage?> GetByIdAsync(
       int imageId,
       CancellationToken cancellationToken);

    void Remove(RoomImage roomImage);


    Task<int> GetNextDisplayOrderAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}
