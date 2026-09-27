using HotelBooking.Domain.Rooms;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class RoomTests
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
    public void Constructor_WhenDataIsValid_ShouldCreateRoom()
    {
        // Act
        var room =
            new Room(
                hotelId: 5,
                roomNumber: "  101  ",
                roomType: RoomType.Deluxe,
                description: "  Sea view  ",
                adultsCapacity: 2,
                childrenCapacity: 1,
                pricePerNight: 120m,
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(5, room.HotelId);
        Assert.Equal("101", room.RoomNumber);
        Assert.Equal(RoomType.Deluxe, room.RoomType);
        Assert.Equal("Sea view", room.Description);
        Assert.Equal(2, room.AdultsCapacity);
        Assert.Equal(1, room.ChildrenCapacity);
        Assert.Equal(120m, room.PricePerNight);
        Assert.Equal(CreatedAt, room.CreatedAt);

        Assert.False(room.IsDeleted);
        Assert.Null(room.DeletedAt);
        Assert.Null(room.UpdatedAt);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsWhitespace_ShouldSetDescriptionToNull()
    {
        // Act
        var room =
            new Room(
                5,
                "101",
                RoomType.Standard,
                "   ",
                2,
                0,
                100m,
                CreatedAt);

        // Assert
        Assert.Null(room.Description);
    }

    [Fact]
    public void Update_WhenRoomIsActive_ShouldUpdateRoom()
    {
        // Arrange
        var room =
            CreateRoom();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        room.Update(
            "  202  ",
            RoomType.Suite,
            "  Large suite  ",
            3,
            2,
            250m,
            updatedAt);

        // Assert
        Assert.Equal("202", room.RoomNumber);
        Assert.Equal(RoomType.Suite, room.RoomType);
        Assert.Equal("Large suite", room.Description);
        Assert.Equal(3, room.AdultsCapacity);
        Assert.Equal(2, room.ChildrenCapacity);
        Assert.Equal(250m, room.PricePerNight);
        Assert.Equal(updatedAt, room.UpdatedAt);
    }

    [Fact]
    public void Update_WhenDescriptionIsWhitespace_ShouldSetDescriptionToNull()
    {
        // Arrange
        var room =
            CreateRoom();

        // Act
        room.Update(
            "101",
            RoomType.Standard,
            "   ",
            2,
            0,
            100m,
            CreatedAt.AddHours(1));

        // Assert
        Assert.Null(room.Description);
    }

    [Fact]
    public void Update_WhenRoomIsDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var room =
            CreateRoom();

        room.Delete(
            CreatedAt.AddHours(1));

        // Act
        var action =
            () => room.Update(
                "202",
                RoomType.Suite,
                "Updated description",
                3,
                1,
                200m,
                CreatedAt.AddHours(2));

        // Assert
        var exception =
            Assert.Throws<InvalidOperationException>(
                action);

        Assert.Equal(
            "A deleted room cannot be updated.",
            exception.Message);
    }

    [Fact]
    public void Update_WhenRoomIsDeleted_ShouldNotModifyExistingValues()
    {
        // Arrange
        var room =
            CreateRoom();

        var deletedAt =
            CreatedAt.AddHours(1);

        room.Delete(
            deletedAt);

        // Act
        var action =
            () => room.Update(
                "999",
                RoomType.PresidentialSuite,
                "Should not be applied",
                10,
                10,
                999m,
                CreatedAt.AddHours(2));

        // Assert
        Assert.Throws<InvalidOperationException>(
            action);

        Assert.Equal("101", room.RoomNumber);
        Assert.Equal(RoomType.Standard, room.RoomType);
        Assert.Equal("Basic room", room.Description);
        Assert.Equal(2, room.AdultsCapacity);
        Assert.Equal(0, room.ChildrenCapacity);
        Assert.Equal(100m, room.PricePerNight);

        Assert.True(room.IsDeleted);
        Assert.Equal(deletedAt, room.DeletedAt);
        Assert.Equal(deletedAt, room.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenRoomIsActive_ShouldSoftDeleteRoom()
    {
        // Arrange
        var room =
            CreateRoom();

        var deletedAt =
            CreatedAt.AddHours(1);

        // Act
        room.Delete(
            deletedAt);

        // Assert
        Assert.True(room.IsDeleted);
        Assert.Equal(deletedAt, room.DeletedAt);
        Assert.Equal(deletedAt, room.UpdatedAt);
    }

    [Fact]
    public void Delete_WhenRoomIsAlreadyDeleted_ShouldBeIdempotent()
    {
        // Arrange
        var room =
            CreateRoom();

        var originalDeletedAt =
            CreatedAt.AddHours(1);

        room.Delete(
            originalDeletedAt);

        // Act
        room.Delete(
            CreatedAt.AddHours(5));

        // Assert
        Assert.True(room.IsDeleted);

        Assert.Equal(
            originalDeletedAt,
            room.DeletedAt);

        Assert.Equal(
            originalDeletedAt,
            room.UpdatedAt);
    }

    private static Room CreateRoom()
    {
        return new Room(
            hotelId: 5,
            roomNumber: "101",
            roomType: RoomType.Standard,
            description: "Basic room",
            adultsCapacity: 2,
            childrenCapacity: 0,
            pricePerNight: 100m,
            createdAt: CreatedAt);
    }
}