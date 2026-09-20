using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Rooms.CreateRoom;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.CreateRoom;

public sealed class CreateRoomCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly CreateRoomCommandValidator _validator = new();

    private CreateRoomCommandHandler CreateHandler()
    {
        return new CreateRoomCommandHandler(
            _validator,
            _hotelRepository.Object,
            _roomRepository.Object,
            _currentUserService.Object);
    }

    private static CreateRoomCommand CreateValidCommand()
    {
        return new CreateRoomCommand(
            HotelId: 1,
            RoomNumber: "101",
            RoomType: RoomType.Standard,
            Description: "Comfortable standard room.",
            AdultsCapacity: 2,
            ChildrenCapacity: 1,
            PricePerNight: 100m);
    }

    private static Hotel CreateHotel(string ownerId = "owner-1")
    {
        return new Hotel(
            "Test Hotel",
            cityId: 1,
            ownerId,
            starRating: 4,
            HotelCategory.Luxury,
            DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.RoomId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("HotelNotFound", error.Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-2");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.RoomId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("HotelOwnershipRequired", error.Code);

        _roomRepository.Verify(
            repository => repository.Add(It.IsAny<Room>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateRoom_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "different-owner");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(true);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberAsync(
                command.HotelId,
                command.RoomNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _roomRepository.Verify(
            repository => repository.Add(
                It.Is<Room>(room =>
                    room.HotelId == command.HotelId &&
                    room.RoomNumber == command.RoomNumber &&
                    room.RoomType == command.RoomType &&
                    room.AdultsCapacity == command.AdultsCapacity &&
                    room.ChildrenCapacity == command.ChildrenCapacity &&
                    room.PricePerNight == command.PricePerNight)),
            Times.Once);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateRoom_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberAsync(
                command.HotelId,
                command.RoomNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _roomRepository.Verify(
            repository => repository.Add(
                It.Is<Room>(room =>
                    room.HotelId == command.HotelId &&
                    room.RoomNumber == command.RoomNumber)),
            Times.Once);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRoomNumberAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberAsync(
                command.HotelId,
                command.RoomNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.RoomId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("RoomNumberAlreadyExists", error.Code);

        _roomRepository.Verify(
            repository => repository.Add(It.IsAny<Room>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenPriceIsNotPositive()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            PricePerNight = 0
        };

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.RoomId);
        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(CreateRoomCommand.PricePerNight));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.Add(It.IsAny<Room>()),
            Times.Never);
    }
}