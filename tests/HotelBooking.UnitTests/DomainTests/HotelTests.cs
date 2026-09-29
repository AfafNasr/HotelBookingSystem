using HotelBooking.Domain.Hotels;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class HotelTests
{
    private static readonly DateTime CreatedAt =
        new(
            2026,
            9,
            27,
            10,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateHotel()
    {
        // Act
        var hotel =
            new Hotel(
                name: "  Grand Hotel  ",
                cityId: 10,
                ownerId: "owner-123",
                starRating: 5,
                category: HotelCategory.Luxury,
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(
            "Grand Hotel",
            hotel.Name);

        Assert.Equal(
            10,
            hotel.CityId);

        Assert.Equal(
            "owner-123",
            hotel.OwnerId);

        Assert.Equal(
            5,
            hotel.StarRating);

        Assert.Equal(
            HotelCategory.Luxury,
            hotel.Category);

        Assert.Equal(
            CreatedAt,
            hotel.CreatedAt);

        Assert.False(
            hotel.IsDeleted);

        Assert.Null(
            hotel.DeletedAt);

        Assert.Null(
            hotel.UpdatedAt);

        Assert.Null(
            hotel.Description);

        Assert.Null(
            hotel.Address);

        Assert.Null(
            hotel.Latitude);

        Assert.Null(
            hotel.Longitude);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateHotel()
    {
        // Arrange
        var hotel =
            CreateHotel();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        hotel.Update(
            "  Updated Hotel  ",
            20,
            "owner-999",
            4,
            HotelCategory.Boutique,
            "  Updated description  ",
            "  Updated address  ",
            31.9500m,
            35.9300m,
            updatedAt);

        // Assert
        Assert.Equal(
            "Updated Hotel",
            hotel.Name);

        Assert.Equal(
            20,
            hotel.CityId);

        Assert.Equal(
            "owner-999",
            hotel.OwnerId);

        Assert.Equal(
            4,
            hotel.StarRating);

        Assert.Equal(
            HotelCategory.Boutique,
            hotel.Category);

        Assert.Equal(
            "Updated description",
            hotel.Description);

        Assert.Equal(
            "Updated address",
            hotel.Address);

        Assert.Equal(
            31.9500m,
            hotel.Latitude);

        Assert.Equal(
            35.9300m,
            hotel.Longitude);

        Assert.Equal(
            updatedAt,
            hotel.UpdatedAt);
    }

    [Fact]
    public void Update_WhenOptionalTextFieldsAreWhitespace_ShouldSetThemToNull()
    {
        // Arrange
        var hotel =
            CreateHotel();

        // Act
        hotel.Update(
            "Grand Hotel",
            10,
            "owner-123",
            5,
            HotelCategory.Luxury,
            "   ",
            "   ",
            null,
            null,
            CreatedAt.AddHours(1));

        // Assert
        Assert.Null(
            hotel.Description);

        Assert.Null(
            hotel.Address);

        Assert.Null(
            hotel.Latitude);

        Assert.Null(
            hotel.Longitude);
    }

    [Fact]
    public void CompleteProfile_WhenDataIsValid_ShouldUpdateOwnerManagedDetails()
    {
        // Arrange
        var hotel =
            CreateHotel();

        var updatedAt =
            CreatedAt.AddHours(2);

        // Act
        hotel.CompleteProfile(
            "  Luxury hotel near city center  ",
            "  Main Street 10  ",
            31.9500m,
            35.9300m,
            updatedAt);

        // Assert
        Assert.Equal(
            "Luxury hotel near city center",
            hotel.Description);

        Assert.Equal(
            "Main Street 10",
            hotel.Address);

        Assert.Equal(
            31.9500m,
            hotel.Latitude);

        Assert.Equal(
            35.9300m,
            hotel.Longitude);

        Assert.Equal(
            updatedAt,
            hotel.UpdatedAt);
    }

    [Fact]
    public void CompleteProfile_WhenOptionalTextFieldsAreWhitespace_ShouldSetThemToNull()
    {
        // Arrange
        var hotel =
            CreateHotel();

        // Act
        hotel.CompleteProfile(
            "   ",
            "   ",
            null,
            null,
            CreatedAt.AddHours(2));

        // Assert
        Assert.Null(
            hotel.Description);

        Assert.Null(
            hotel.Address);

        Assert.Null(
            hotel.Latitude);

        Assert.Null(
            hotel.Longitude);
    }

    [Fact]
    public void Delete_WhenHotelIsActive_ShouldSoftDeleteHotel()
    {
        // Arrange
        var hotel =
            CreateHotel();

        var deletedAt =
            CreatedAt.AddHours(1);

        // Act
        hotel.Delete(
            deletedAt);

        // Assert
        Assert.True(
            hotel.IsDeleted);

        Assert.Equal(
            deletedAt,
            hotel.DeletedAt);

        Assert.Equal(
            deletedAt,
            hotel.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenHotelIsAlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var hotel =
            CreateHotel();

        var originalDeletedAt =
            CreatedAt.AddHours(1);

        hotel.Delete(
            originalDeletedAt);

        // Act
        hotel.Delete(
            CreatedAt.AddHours(5));

        // Assert
        Assert.True(
            hotel.IsDeleted);

        Assert.Equal(
            originalDeletedAt,
            hotel.DeletedAt);

        Assert.Equal(
            originalDeletedAt,
            hotel.UpdatedAt);
    }

    private static Hotel CreateHotel()
    {
        return new Hotel(
            name: "Grand Hotel",
            cityId: 10,
            ownerId: "owner-123",
            starRating: 5,
            category: HotelCategory.Luxury,
            createdAt: CreatedAt);
    }
}