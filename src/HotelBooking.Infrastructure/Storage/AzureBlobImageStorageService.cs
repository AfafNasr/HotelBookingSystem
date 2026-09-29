using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotelBooking.Application.Common.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureBlobImageStorageService
    : IImageStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<AzureBlobImageStorageService> _logger;

    public AzureBlobImageStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options,
        ILogger<AzureBlobImageStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
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
        try
        {
            var containerClient =
                GetContainerClient(container);

            var blobClient =
                containerClient.GetBlobClient(storageKey);

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
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to upload image to Azure Blob Storage. Container {Container}, StorageKey {StorageKey}.",
                container,
                storageKey);

            throw;
        }
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