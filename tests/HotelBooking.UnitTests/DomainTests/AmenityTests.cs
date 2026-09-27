using HotelBooking.Domain.Amenities;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class AmenityTests
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
    public void Constructor_WhenDataIsValid_ShouldCreateAmenity()
    {
        // Act
        var amenity =
            new Amenity(
                "  Free WiFi  ",
                CreatedAt);

        // Assert
        Assert.Equal(
            "Free WiFi",
            amenity.Name);

        Assert.Equal(
            CreatedAt,
            amenity.CreatedAt);

        Assert.Null(
            amenity.UpdatedAt);

        Assert.False(
            amenity.IsDeleted);

        Assert.Null(
            amenity.DeletedAt);
    }

    [Fact]
    public void Update_WhenNameIsValid_ShouldUpdateAmenity()
    {
        // Arrange
        var amenity =
            CreateAmenity();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        amenity.Update(
            "  Swimming Pool  ",
            updatedAt);

        // Assert
        Assert.Equal(
            "Swimming Pool",
            amenity.Name);

        Assert.Equal(
            updatedAt,
            amenity.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenNameIsEmpty_ShouldThrowArgumentException(
        string name)
    {
        // Arrange
        var amenity =
            CreateAmenity();

        // Act
        var action =
            () => amenity.Update(
                name,
                CreatedAt.AddHours(1));

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "name",
            exception.ParamName);

        Assert.Equal(
            "Free WiFi",
            amenity.Name);

        Assert.Null(
            amenity.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenAmenityIsActive_ShouldSoftDeleteAmenity()
    {
        // Arrange
        var amenity =
            CreateAmenity();

        var deletedAt =
            CreatedAt.AddHours(1);

        // Act
        amenity.Delete(
            deletedAt);

        // Assert
        Assert.True(
            amenity.IsDeleted);

        Assert.Equal(
            deletedAt,
            amenity.DeletedAt);

        Assert.Equal(
            deletedAt,
            amenity.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenAmenityIsAlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var amenity =
            CreateAmenity();

        var originalDeletedAt =
            CreatedAt.AddHours(1);

        amenity.Delete(
            originalDeletedAt);

        // Act
        amenity.Delete(
            CreatedAt.AddHours(5));

        // Assert
        Assert.True(
            amenity.IsDeleted);

        Assert.Equal(
            originalDeletedAt,
            amenity.DeletedAt);

        Assert.Equal(
            originalDeletedAt,
            amenity.UpdatedAt);
    }

    private static Amenity CreateAmenity()
    {
        return new Amenity(
            "Free WiFi",
            CreatedAt);
    }
}