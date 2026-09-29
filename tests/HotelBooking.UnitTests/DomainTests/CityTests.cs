using HotelBooking.Domain.Cities;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class CityTests
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
    public void Constructor_WhenDataIsValid_ShouldCreateCity()
    {
        // Act
        var city =
            new City(
                "  Amman  ",
                "  jo  ",
                "  Central Post Office  ",
                CreatedAt);

        // Assert
        Assert.Equal(
            "Amman",
            city.Name);

        Assert.Equal(
            "JO",
            city.CountryCode);

        Assert.Equal(
            "Central Post Office",
            city.PostOffice);

        Assert.Equal(
            CreatedAt,
            city.CreatedAt);

        Assert.False(
            city.IsDeleted);

        Assert.Null(
            city.DeletedAt);

        Assert.Null(
            city.UpdatedAt);

        Assert.Null(
            city.ThumbnailStorageKey);
    }

    [Fact]
    public void Constructor_WhenPostOfficeIsWhitespace_ShouldSetPostOfficeToNull()
    {
        // Act
        var city =
            new City(
                "Amman",
                "JO",
                "   ",
                CreatedAt);

        // Assert
        Assert.Null(
            city.PostOffice);
    }

    [Fact]
    public void Constructor_ShouldNormalizeCountryCodeToUppercase()
    {
        // Act
        var city =
            new City(
                "Amman",
                "  jo  ",
                null,
                CreatedAt);

        // Assert
        Assert.Equal(
            "JO",
            city.CountryCode);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateCity()
    {
        // Arrange
        var city =
            CreateCity();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        city.Update(
            "  Nablus  ",
            "  ps  ",
            "  Nablus Central  ",
            updatedAt);

        // Assert
        Assert.Equal(
            "Nablus",
            city.Name);

        Assert.Equal(
            "PS",
            city.CountryCode);

        Assert.Equal(
            "Nablus Central",
            city.PostOffice);

        Assert.Equal(
            updatedAt,
            city.UpdatedAt);
    }

    [Fact]
    public void Update_WhenPostOfficeIsWhitespace_ShouldSetPostOfficeToNull()
    {
        // Arrange
        var city =
            CreateCity();

        // Act
        city.Update(
            "Amman",
            "JO",
            "   ",
            CreatedAt.AddHours(1));

        // Assert
        Assert.Null(
            city.PostOffice);
    }

    [Fact]
    public void Delete_WhenCityIsActive_ShouldSoftDeleteCity()
    {
        // Arrange
        var city =
            CreateCity();

        var deletedAt =
            CreatedAt.AddHours(1);

        // Act
        city.Delete(
            deletedAt);

        // Assert
        Assert.True(
            city.IsDeleted);

        Assert.Equal(
            deletedAt,
            city.DeletedAt);

        Assert.Equal(
            deletedAt,
            city.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenCityIsAlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var city =
            CreateCity();

        var originalDeletedAt =
            CreatedAt.AddHours(1);

        city.Delete(
            originalDeletedAt);

        // Act
        city.Delete(
            CreatedAt.AddHours(5));

        // Assert
        Assert.True(
            city.IsDeleted);

        Assert.Equal(
            originalDeletedAt,
            city.DeletedAt);

        Assert.Equal(
            originalDeletedAt,
            city.UpdatedAt);
    }

    private static City CreateCity()
    {
        return new City(
            "Amman",
            "JO",
            "Central Post Office",
            CreatedAt);
    }
}