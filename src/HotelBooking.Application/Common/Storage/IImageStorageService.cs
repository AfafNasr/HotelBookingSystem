namespace HotelBooking.Application.Common.Storage;

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