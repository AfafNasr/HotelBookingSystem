using HotelBooking.Domain.Hotels;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class HotelImageTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateHotelImage()
    {
        // Act
        var image =
            new HotelImage(
                hotelId: 10,
                storageKey: "hotel-10/image-1.jpg",
                displayOrder: 1,
                isPrimary: true);

        // Assert
        Assert.Equal(10, image.HotelId);
        Assert.Equal("hotel-10/image-1.jpg", image.StorageKey);
        Assert.Equal(1, image.DisplayOrder);
        Assert.True(image.IsPrimary);
    }

    [Fact]
    public void ReplaceStorageKey_WhenStorageKeyIsValid_ShouldReplaceAndTrimStorageKey()
    {
        // Arrange
        var image =
            CreateImage();

        // Act
        image.ReplaceStorageKey(
            "  hotel-10/new-image.jpg  ");

        // Assert
        Assert.Equal(
            "hotel-10/new-image.jpg",
            image.StorageKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReplaceStorageKey_WhenStorageKeyIsEmpty_ShouldThrowArgumentException(
        string storageKey)
    {
        // Arrange
        var image =
            CreateImage();

        var originalStorageKey =
            image.StorageKey;

        // Act
        var action =
            () => image.ReplaceStorageKey(
                storageKey);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(
                action);

        Assert.Equal(
            "storageKey",
            exception.ParamName);

        Assert.Equal(
            originalStorageKey,
            image.StorageKey);
    }

    private static HotelImage CreateImage()
    {
        return new HotelImage(
            hotelId: 10,
            storageKey: "hotel-10/image-1.jpg",
            displayOrder: 1,
            isPrimary: true);
    }
}