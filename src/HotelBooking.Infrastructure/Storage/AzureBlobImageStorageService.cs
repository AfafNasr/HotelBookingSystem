using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Storage;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureBlobImageStorageService
    : IImageStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;

    public AzureBlobImageStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
    }
    private BlobContainerClient GetContainerClient(
    ImageContainer container)
    {
        var containerName = container switch
        {
            ImageContainer.HotelImages =>
                _options.HotelImagesContainer,

            ImageContainer.RoomImages =>
                _options.RoomImagesContainer,

            _ => throw new ArgumentOutOfRangeException(
                nameof(container),
                container,
                "Unsupported image container.")
        };

        return _blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task UploadAsync(
    ImageContainer container,
    string storageKey,
    Stream content,
    string contentType,
    CancellationToken cancellationToken)
    {
        var containerClient = GetContainerClient(container);

        var blobClient = containerClient.GetBlobClient(storageKey);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(
            content,
            options,
            cancellationToken);
    }

    public async Task DeleteAsync(
     ImageContainer container,
     string storageKey,
     CancellationToken cancellationToken)
    {
        var containerClient = GetContainerClient(container);

        var blobClient = containerClient.GetBlobClient(storageKey);

        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);
    }
}