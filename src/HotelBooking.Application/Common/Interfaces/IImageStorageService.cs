namespace HotelBooking.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken);
}