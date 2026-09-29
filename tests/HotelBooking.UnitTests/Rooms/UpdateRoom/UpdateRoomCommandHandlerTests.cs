using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.UpdateRoom;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly UpdateRoomCommandValidator _validator = new();

    private UpdateRoomCommandHandler CreateHandler()
    {
        return new UpdateRoomCommandHandler(
            _validator,
            _roomRepository.Object,
            _hotelRepository.Object,
            _currentUserService.Object,
            _timeProvider);
    }

    private static UpdateRoomCommand CreateValidCommand()
    {
        return new UpdateRoomCommand(
            RoomId: 1,
            RoomNumber: "202",
            RoomType: RoomType.Deluxe,
            Description: "Updated room description.",
            AdultsCapacity: 3,
            ChildrenCapacity: 2,
            PricePerNight: 175m);
    }

    private static Room CreateRoom()
    {
        return new Room(
            hotelId: 1,
            roomNumber: "101",
            roomType: RoomType.Standard,
            description: "Original description.",
            adultsCapacity: 2,
            childrenCapacity: 1,
            pricePerNight: 100m,
            createdAt: DateTime.UtcNow);
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
    public async Task HandleAsync_ShouldReturnValidationError_WhenCommandIsInvalid()
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

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(UpdateRoomCommand.PricePerNight));

        _roomRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Room.NotFound", error.Code);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();
        var room = CreateRoom();

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                room.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.NotFound", error.Code);

        _roomRepository.Verify(
            repository => repository.ExistsByRoomNumberExceptAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var room = CreateRoom();
        var hotel = CreateHotel(ownerId: "owner-2");

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                room.HotelId,
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

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.ManagementForbidden", error.Code);

        _roomRepository.Verify(
            repository => repository.ExistsByRoomNumberExceptAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRoomNumberAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();
        var room = CreateRoom();
        var hotel = CreateHotel(ownerId: "owner-1");

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                room.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberExceptAsync(
                room.HotelId,
                command.RoomNumber,
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Room.NumberAlreadyExists", error.Code);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateRoom_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var room = CreateRoom();
        var hotel = CreateHotel(ownerId: "owner-1");

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                room.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberExceptAsync(
                room.HotelId,
                command.RoomNumber,
                room.Id,
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

        Assert.Equal(command.RoomNumber, room.RoomNumber);
        Assert.Equal(command.RoomType, room.RoomType);
        Assert.Equal(command.Description, room.Description);
        Assert.Equal(command.AdultsCapacity, room.AdultsCapacity);
        Assert.Equal(command.ChildrenCapacity, room.ChildrenCapacity);
        Assert.Equal(command.PricePerNight, room.PricePerNight);
        Assert.NotNull(room.UpdatedAt);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateRoom_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = CreateValidCommand();
        var room = CreateRoom();
        var hotel = CreateHotel(ownerId: "different-owner");

        _roomRepository
            .Setup(repository => repository.GetByIdAsync(
                command.RoomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                room.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(true);

        _roomRepository
            .Setup(repository => repository.ExistsByRoomNumberExceptAsync(
                room.HotelId,
                command.RoomNumber,
                room.Id,
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

        Assert.Equal(command.RoomNumber, room.RoomNumber);
        Assert.Equal(command.RoomType, room.RoomType);
        Assert.Equal(command.PricePerNight, room.PricePerNight);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}