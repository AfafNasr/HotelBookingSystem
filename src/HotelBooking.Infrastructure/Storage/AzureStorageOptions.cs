namespace HotelBooking.Infrastructure.Storage;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string AccountName { get; init; } = null!;

    public string HotelImagesContainer { get; init; } = null!;
    public string RoomImagesContainer { get; init; } = null!;
}