namespace HotelBooking.Infrastructure.Hotels;

public sealed class GeoapifyOptions
{
    public const string SectionName = "Geoapify";

    public string BaseUrl { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
}