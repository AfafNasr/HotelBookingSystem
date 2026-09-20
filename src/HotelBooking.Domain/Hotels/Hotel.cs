using HotelBooking.Domain.Cities;

namespace HotelBooking.Domain.Hotels;

public sealed class Hotel
{
    public int Id { get; private set; }

    // Admin-managed core data
    public string Name { get; private set; } = null!;
    public int CityId { get; private set; }
    public string OwnerId { get; private set; } = null!;
    public int StarRating { get; private set; }
    public HotelCategory Category { get; private set; }

    // Owner-managed details
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }

    // System-managed
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public City City { get; private set; } = null!;

    private Hotel()
    {
    }

    public Hotel(
        string name,
        int cityId,
        string ownerId,
        int starRating,
        HotelCategory category,
        DateTime createdAt)
    {
        Name = name.Trim();
        CityId = cityId;
        OwnerId = ownerId;
        StarRating = starRating;
        Category = category;

        CreatedAt = createdAt;
        IsDeleted = false;
    }

    public void Update(
    string name,
    int cityId,
    string ownerId,
    int starRating,
    HotelCategory category,
    decimal? latitude,
    decimal? longitude,
    DateTime updatedAt)
    {
        Name = name.Trim();
        CityId = cityId;
        OwnerId = ownerId;
        StarRating = starRating;
        Category = category;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = updatedAt;
    }

    public void CompleteProfile(
    string? description,
    string? address,
    decimal? latitude,
    decimal? longitude,
    DateTime updatedAt)
    {
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        Address = string.IsNullOrWhiteSpace(address)
            ? null
            : address.Trim();

        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = updatedAt;
    }
}