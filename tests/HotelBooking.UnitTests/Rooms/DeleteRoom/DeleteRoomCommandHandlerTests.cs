using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.DeleteRoom;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly DeleteRoomCommandValidator _validator = new();

    private DeleteRoomCommandHandler CreateHandler()
    {
        return new DeleteRoomCommandHandler(
            _validator,
            _roomRepository.Object,
            _hotelRepository.Object,
            _currentUserService.Object,
            _timeProvider);
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
    public async Task HandleAsync_ShouldReturnValidationError_WhenRoomIdIsInvalid()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(DeleteRoomCommand.RoomId));

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
        var command = new DeleteRoomCommand(RoomId: 1);

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
        var command = new DeleteRoomCommand(RoomId: 1);
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
            repository => repository.HasActiveOrUpcomingBookingsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
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
        var command = new DeleteRoomCommand(RoomId: 1);
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
            repository => repository.HasActiveOrUpcomingBookingsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRoomHasActiveOrUpcomingBookings()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 1);
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
            .Setup(repository => repository.HasActiveOrUpcomingBookingsAsync(
                room.Id,
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
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

        Assert.Equal("Room.HasActiveBookings", error.Code);

        Assert.False(room.IsDeleted);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteRoom_WhenOwnerOwnsHotelAndRoomHasNoActiveBookings()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 1);
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
            .Setup(repository => repository.HasActiveOrUpcomingBookingsAsync(
                room.Id,
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
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

        Assert.True(room.IsDeleted);
        Assert.NotNull(room.DeletedAt);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteRoom_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = new DeleteRoomCommand(RoomId: 1);
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
            .Setup(repository => repository.HasActiveOrUpcomingBookingsAsync(
                room.Id,
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
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

        Assert.True(room.IsDeleted);
        Assert.NotNull(room.DeletedAt);

        _roomRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}