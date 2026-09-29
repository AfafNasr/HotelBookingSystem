using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Application.Rooms.DeleteRoomImage;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace HotelBooking.UnitTests.Rooms.RoomImages;

public sealed class DeleteRoomImageCommandHandlerTests
{
    private const int HotelId = 10;
    private const int RoomId = 20;
    private const int ImageId = 30;

    private const string OwnerId = "owner-1";

    private const string StorageKey =
        "20/image.jpg";

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

    private readonly Mock<ILogger<DeleteRoomImageCommandHandler>>
        _logger = new();

    private readonly DeleteRoomImageCommandValidator
        _validator = new();

    [Fact]
    public async Task HandleAsync_WhenOwnerDeletesImage_ShouldRemoveDatabaseRecordAndDeleteBlob()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteRoomImageCommand(
                    RoomId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _roomImageRepository.Verify(
            repository =>
                repository.Remove(image),
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
                    StorageKey,
                    CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAnotherOwnerTriesToDeleteImage_ShouldReturnForbidden()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteRoomImageCommand(
                    RoomId,
                    ImageId),
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
                repository.Remove(
                    It.IsAny<RoomImage>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenImageBelongsToAnotherRoom_ShouldReturnNotFound()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteRoomImageCommand(
                    RoomId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "RoomImage.NotFound",
            error.Code);

        _roomImageRepository.Verify(
            repository =>
                repository.Remove(
                    It.IsAny<RoomImage>()),
            Times.Never);

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenDatabaseSaveFails_ShouldNotDeleteAzureBlob()
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

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        new DeleteRoomImageCommand(
                            RoomId,
                            ImageId),
                        CancellationToken.None));

        // Assert
        Assert.Equal(
            "Database failure",
            exception.Message);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    It.IsAny<ImageContainer>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAzureDeleteFails_ShouldStillReturnSuccess()
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
                    StorageKey,
                    CancellationToken.None))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Azure failure"));

        var handler = CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteRoomImageCommand(
                    RoomId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _roomImageRepository.Verify(
            repository =>
                repository.Remove(image),
            Times.Once);

        _roomImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRoomDoesNotExist_ShouldReturnNotFound()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteRoomImageCommand(
                    RoomId,
                    ImageId),
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

    private DeleteRoomImageCommandHandler CreateHandler()
    {
        return new DeleteRoomImageCommandHandler(
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
                StorageKey,
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