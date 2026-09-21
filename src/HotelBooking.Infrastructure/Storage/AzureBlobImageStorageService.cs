using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HotelBooking.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureBlobImageStorageService
    : IImageStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobImageStorageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient(
            options.Value.HotelImagesContainer);
    }

    public async Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var blobClient =
            _containerClient.GetBlobClient(storageKey);

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        var blobClient =
            _containerClient.GetBlobClient(storageKey);

        await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);
    }
}