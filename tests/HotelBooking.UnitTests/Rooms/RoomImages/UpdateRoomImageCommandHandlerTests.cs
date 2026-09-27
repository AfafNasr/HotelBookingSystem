using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.UpdateRoomImage;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace HotelBooking.UnitTests.Rooms.RoomImages;

public sealed class UpdateRoomImageCommandHandlerTests
{
    private const int HotelId = 10;
    private const int RoomId = 20;
    private const int ImageId = 30;

    private const string OwnerId = "owner-1";

    private const string OldStorageKey =
        "20/old-image.jpg";

    private readonly Mock<IRoomRepository>
        _roomRepository = new();

    private readonly Mock<IHotelRepository>
        _hotelRepository = new();

    private readonly Mock<IRoomImageRepository>
        _roomImageRepository = new();

    private readonly Mock<IImageStorageService>
        _imageStorageService = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly Mock<ILogger<UpdateRoomImageCommandHandler>>
        _logger = new();

    private readonly UpdateRoomImageCommandValidator
        _validator = new();

    [Fact]
    public async Task HandleAsync_WhenOwnerReplacesImage_ShouldUploadSaveAndDeleteOldBlob()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();
        var image = CreateRoomImage();

        SetupOwner();

        SetupHotel(hotel);
        SetupRoom(room);
        SetupImage(image);

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        var command =
            CreateCommand(stream);

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotEqual(
            OldStorageKey,
            image.StorageKey);

        Assert.StartsWith(
            $"{RoomId}/",
            image.StorageKey);

        Assert.EndsWith(
            ".jpg",
            image.StorageKey);

        _imageStorageService.Verify(
            service =>
                service.UploadAsync(
                    ImageContainer.RoomImages,
                    image.StorageKey,
                    stream,
                    "image/jpeg",
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.RoomImages,
                    OldStorageKey,
                    CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAnotherOwnerTriesToReplaceImage_ShouldReturnForbidden()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("another-owner");

        _currentUserService
            .Setup(service =>
                service.IsInRole(Roles.Admin))
            .Returns(false);

        SetupHotel(hotel);
        SetupRoom(room);

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.ManagementForbidden",
            error.Code);

        _roomImageRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenImageBelongsToAnotherRoom_ShouldReturnNotFoundWithoutUploading()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();

        var image =
            new RoomImage(
                roomId: 999,
                storageKey: "999/image.jpg",
                displayOrder: 0,
                isPrimary: false);

        SetId(
            image,
            ImageId);

        SetupOwner();

        SetupHotel(hotel);
        SetupRoom(room);
        SetupImage(image);

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "RoomImage.NotFound",
            error.Code);

        _imageStorageService.VerifyNoOtherCalls();

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenImageSignatureIsInvalid_ShouldReturnValidationError()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();
        var image = CreateRoomImage();

        SetupOwner();

        SetupHotel(hotel);
        SetupRoom(room);
        SetupImage(image);

        var handler = CreateHandler();

        using var stream =
            new MemoryStream(
                new byte[12]);

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "InvalidImageContent",
            error.Code);

        _imageStorageService.VerifyNoOtherCalls();

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDatabaseSaveFails_ShouldDeleteNewBlobAndRethrow()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();
        var image = CreateRoomImage();

        SetupOwner();

        SetupHotel(hotel);
        SetupRoom(room);
        SetupImage(image);

        _roomImageRepository
            .Setup(repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure"));

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        CreateCommand(stream),
                        CancellationToken.None));

        // Assert
        Assert.Equal(
            "Database failure",
            exception.Message);

        var newStorageKey =
            image.StorageKey;

        Assert.NotEqual(
            OldStorageKey,
            newStorageKey);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.RoomImages,
                    newStorageKey,
                    CancellationToken.None),
            Times.Once);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.RoomImages,
                    OldStorageKey,
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDeletingOldBlobFails_ShouldStillReturnSuccess()
    {
        // Arrange
        var hotel = CreateHotel();
        var room = CreateRoom();
        var image = CreateRoomImage();

        SetupOwner();

        SetupHotel(hotel);
        SetupRoom(room);
        SetupImage(image);

        _imageStorageService
            .Setup(service =>
                service.DeleteAsync(
                    ImageContainer.RoomImages,
                    OldStorageKey,
                    CancellationToken.None))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Azure failure"));

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        Assert.NotEqual(
            OldStorageKey,
            image.StorageKey);

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ShouldReturnNotFoundWithoutUsingStorage()
    {
        // Arrange
        SetupOwner();

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler = CreateHandler();

        using var stream =
            CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Room.NotFound",
            error.Code);

        _hotelRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    private UpdateRoomImageCommandHandler CreateHandler()
    {
        return new UpdateRoomImageCommandHandler(
            _validator,
            _roomRepository.Object,
            _hotelRepository.Object,
            _roomImageRepository.Object,
            _imageStorageService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    private void SetupOwner()
    {
        _currentUserService
            .Setup(service => service.UserId)
            .Returns(OwnerId);

        _currentUserService
            .Setup(service =>
                service.IsInRole(Roles.Admin))
            .Returns(false);
    }

    private void SetupRoom(Room room)
    {
        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);
    }

    private void SetupHotel(Hotel hotel)
    {
        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);
    }

    private void SetupImage(RoomImage image)
    {
        _roomImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
    }

    private static UpdateRoomImageCommand CreateCommand(
        Stream stream)
    {
        return new UpdateRoomImageCommand(
            RoomId,
            ImageId,
            stream,
            "replacement.jpg",
            "image/jpeg",
            stream.Length);
    }

    private static MemoryStream CreateValidJpegStream()
    {
        byte[] bytes =
        [
            0xFF,
            0xD8,
            0xFF,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00
        ];

        return new MemoryStream(bytes);
    }

    private static Hotel CreateHotel()
    {
        var hotel =
            new Hotel(
                "Test Hotel",
                cityId: 1,
                ownerId: OwnerId,
                starRating: 5,
                category: HotelCategory.Luxury,
                createdAt: DateTime.UtcNow);

        SetId(
            hotel,
            HotelId);

        return hotel;
    }

    private static Room CreateRoom()
    {
        var room =
            new Room(
                HotelId,
                "101",
                RoomType.Standard,
                "Test room",
                adultsCapacity: 2,
                childrenCapacity: 1,
                pricePerNight: 100m,
                createdAt: DateTime.UtcNow);

        SetId(
            room,
            RoomId);

        return room;
    }

    private static RoomImage CreateRoomImage()
    {
        var image =
            new RoomImage(
                RoomId,
                OldStorageKey,
                displayOrder: 0,
                isPrimary: false);

        SetId(
            image,
            ImageId);

        return image;
    }

    private static void SetId<T>(
        T entity,
        int id)
    {
        var field =
            typeof(T).GetField(
                "<Id>k__BackingField",
                BindingFlags.Instance |
                BindingFlags.NonPublic);

        if (field is null)
        {
            throw new InvalidOperationException(
                $"Could not find Id backing field on {typeof(T).Name}.");
        }

        field.SetValue(
            entity,
            id);
    }
}