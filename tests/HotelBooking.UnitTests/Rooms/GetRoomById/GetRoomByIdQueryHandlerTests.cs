using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.GetRoomById;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.GetRoomById;

public sealed class GetRoomByIdQueryHandlerTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();

    private GetRoomByIdQueryHandler CreateHandler()
    {
        return new GetRoomByIdQueryHandler(
            _roomRepository.Object);
    }

    private static Room CreateRoom()
    {
        return new Room(
            hotelId: 1,
            roomNumber: "101",
            roomType: RoomType.Standard,
            description: "Comfortable standard room.",
            adultsCapacity: 2,
            childrenCapacity: 1,
            pricePerNight: 100m,
            createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        var query = new GetRoomByIdQuery(
            RoomId: 1);

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                query.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Room);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Room.NotFound", error.Code);

        _roomRepository.Verify(
            repository => repository.GetByIdAsync(
                query.RoomId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRoomDetails_WhenRoomExists()
    {
        // Arrange
        var query = new GetRoomByIdQuery(
            RoomId: 1);

        var room = CreateRoom();

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                query.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        var roomDetails = Assert.IsType<RoomDetails>(
            result.Room);

        Assert.Equal(room.Id, roomDetails.Id);
        Assert.Equal(room.HotelId, roomDetails.HotelId);
        Assert.Equal(room.RoomNumber, roomDetails.RoomNumber);
        Assert.Equal(room.RoomType, roomDetails.RoomType);
        Assert.Equal(room.Description, roomDetails.Description);
        Assert.Equal(room.AdultsCapacity, roomDetails.AdultsCapacity);
        Assert.Equal(room.ChildrenCapacity, roomDetails.ChildrenCapacity);
        Assert.Equal(room.PricePerNight, roomDetails.PricePerNight);

        _roomRepository.Verify(
            repository => repository.GetByIdAsync(
                query.RoomId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}