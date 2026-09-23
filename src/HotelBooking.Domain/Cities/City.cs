namespace HotelBooking.Domain.Cities;

public class City
{
    public int Id { get; private set; }

    public string Name { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string? PostOffice { get; private set; }

    public string? ThumbnailStorageKey { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public Country Country { get; private set; } = null!;



    private City()
    {
    }

    public City(
        string name,
        string countryCode,
        string? postOffice,
        DateTime createdAt)
    {
        Name = name.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();

        PostOffice = string.IsNullOrWhiteSpace(postOffice)
            ? null
            : postOffice.Trim();

        CreatedAt = createdAt;
        IsDeleted = false;
    }

    public void Update(
    string name,
    string countryCode,
    string? postOffice,
    DateTime updatedAt)
    {
        Name = name.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();

        PostOffice = string.IsNullOrWhiteSpace(postOffice)
            ? null
            : postOffice.Trim();

        UpdatedAt = updatedAt;
    }
}