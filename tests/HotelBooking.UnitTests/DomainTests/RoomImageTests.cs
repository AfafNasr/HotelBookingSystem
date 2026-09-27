using HotelBooking.Domain.Rooms;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class RoomImageTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateRoomImage()
    {
        // Act
        var image =
            new RoomImage(
                roomId: 20,
                storageKey: "room-20/image-1.jpg",
                displayOrder: 1,
                isPrimary: false);

        // Assert
        Assert.Equal(20, image.RoomId);
        Assert.Equal("room-20/image-1.jpg", image.StorageKey);
        Assert.Equal(1, image.DisplayOrder);
        Assert.False(image.IsPrimary);
    }

    [Fact]
    public void ReplaceStorageKey_WhenStorageKeyIsValid_ShouldReplaceAndTrimStorageKey()
    {
        // Arrange
        var image =
            CreateImage();

        // Act
        image.ReplaceStorageKey(
            "  room-20/new-image.jpg  ");

        // Assert
        Assert.Equal(
            "room-20/new-image.jpg",
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

    private static RoomImage CreateImage()
    {
        return new RoomImage(
            roomId: 20,
            storageKey: "room-20/image-1.jpg",
            displayOrder: 1,
            isPrimary: false);
    }
}