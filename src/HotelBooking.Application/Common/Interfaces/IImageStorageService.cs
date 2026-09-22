using HotelBooking.Application.Common.Storage;

namespace HotelBooking.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task UploadAsync(
        ImageContainer container,
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        ImageContainer container,
        string storageKey,
        CancellationToken cancellationToken);
}