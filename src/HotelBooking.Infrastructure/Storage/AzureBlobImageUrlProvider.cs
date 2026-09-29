using Azure.Storage.Blobs;
using HotelBooking.Application.Common.Storage;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureBlobImageUrlProvider
    : IImageUrlProvider
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;

    public AzureBlobImageUrlProvider(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
    }

    public string GetUrl(
        ImageContainer container,
        string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            storageKey);

        var containerName =
            container switch
            {
                ImageContainer.HotelImages =>
                    _options.HotelImagesContainer,

                ImageContainer.RoomImages =>
                    _options.RoomImagesContainer,

                ImageContainer.CityImages => _options.CityImagesContainer,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(container),
                    container,
                    "Unsupported image container.")
            };

        return _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(storageKey)
            .Uri
            .AbsoluteUri;
    }
}