namespace HotelBooking.Application.Common.Storage;

public interface IImageUrlProvider
{
    string GetUrl(
        ImageContainer container,
        string storageKey);
}