using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.UploadRoomImage;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBooking.UnitTests.Rooms.UploadRoomImage;

public sealed class UploadRoomImageCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IRoomImageRepository> _roomImageRepository = new();
    private readonly Mock<IImageStorageService> _imageStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly Mock<ILogger<UploadRoomImageCommandHandler>> _logger =
        new();

    private readonly UploadRoomImageCommandValidator _validator = new();

    private UploadRoomImageCommandHandler CreateHandler()
    {
        return new UploadRoomImageCommandHandler(
            _validator,
            _roomRepository.Object,
            _hotelRepository.Object,
            _roomImageRepository.Object,
            _imageStorageService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    private static UploadRoomImageCommand CreateValidCommand()
    {
        byte[] jpegContent =
        [
            0xFF, 0xD8, 0xFF, 0xE0,
            0x00, 0x10, 0x4A, 0x46,
            0x49, 0x46, 0x00, 0x01
        ];

        return new UploadRoomImageCommand(
            RoomId: 1,
            Content: new MemoryStream(jpegContent),
            FileName: "room.jpg",
            ContentType: "image/jpeg",
            Length: jpegContent.Length);
    }

    private static Room CreateRoom()
    {
        return new Room(
            hotelId: 1,
            roomNumber: "101",
            roomType: RoomType.Standard,
            description: "Comfortable room.",
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
            RoomId = 0
        };

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ImageId);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(UploadRoomImageCommand.RoomId));

        _roomRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
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
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Room.NotFound", error.Code);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
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
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.NotFound", error.Code);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
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
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.ManagementForbidden", error.Code);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenImageSignatureIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Content = new MemoryStream(
                [0x01, 0x02, 0x03, 0x04]),
            Length = 4
        };

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

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InvalidImageContent", error.Code);

        _roomImageRepository.Verify(
            repository => repository.GetNextDisplayOrderAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUploadAndPersistImage_WhenOwnerOwnsHotel()
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

        _roomImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _imageStorageService.Verify(
            storage => storage.UploadAsync(
                ImageContainer.RoomImages,
                It.Is<string>(key =>
                    key.StartsWith($"{room.Id}/") &&
                    key.EndsWith(".jpg")),
                command.Content,
                command.ContentType,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _roomImageRepository.Verify(
            repository => repository.Add(
                It.Is<RoomImage>(image =>
                    image.RoomId == room.Id &&
                    image.DisplayOrder == 3 &&
                    !image.IsPrimary)),
            Times.Once);

        _roomImageRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteUploadedImageAndRethrow_WhenSaveChangesFails()
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

        _roomImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _roomImageRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Database save failed."));

        string? uploadedStorageKey = null;

        _imageStorageService
            .Setup(storage => storage.UploadAsync(
                ImageContainer.RoomImages,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<ImageContainer, string, Stream, string, CancellationToken>(
                (_, storageKey, _, _, _) =>
                    uploadedStorageKey = storageKey)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                command,
                CancellationToken.None));

        // Assert
        Assert.NotNull(uploadedStorageKey);

        _imageStorageService.Verify(
            storage => storage.DeleteAsync(
                ImageContainer.RoomImages,
                uploadedStorageKey!,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}