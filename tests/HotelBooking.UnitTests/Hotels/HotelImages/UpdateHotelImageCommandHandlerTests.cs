using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.UpdateHotelImage;
using HotelBooking.Domain.Hotels;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBooking.UnitTests.Hotels.HotelImages;

public sealed class UpdateHotelImageCommandHandlerTests
{
    private const int HotelId = 10;
    private const int ImageId = 20;
    private const string OwnerId = "owner-1";
    private const string OldStorageKey = "10/old-image.jpg";

    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IHotelImageRepository> _hotelImageRepository = new();
    private readonly Mock<IImageStorageService> _imageStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly Mock<ILogger<UpdateHotelImageCommandHandler>>
        _logger = new();

    private readonly UpdateHotelImageCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenOwnerReplacesImage_ShouldUploadNewImageSaveDatabaseAndDeleteOldBlob()
    {
        // Arrange
        var hotel = CreateHotel();
        var image = CreateHotelImage();

        SetupOwner();

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

        var command = CreateCommand(stream);

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotEqual(
            OldStorageKey,
            image.StorageKey);

        Assert.StartsWith(
            $"{HotelId}/",
            image.StorageKey);

        Assert.EndsWith(
            ".jpg",
            image.StorageKey);

        _imageStorageService.Verify(
            service =>
                service.UploadAsync(
                    ImageContainer.HotelImages,
                    image.StorageKey,
                    stream,
                    "image/jpeg",
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.HotelImages,
                    OldStorageKey,
                    CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenDatabaseSaveFails_ShouldDeleteNewBlobAndRethrow()
    {
        // Arrange
        var hotel = CreateHotel();
        var image = CreateHotelImage();

        SetupOwner();

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        _hotelImageRepository
            .Setup(repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure"));

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(
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
                service.UploadAsync(
                    ImageContainer.HotelImages,
                    newStorageKey,
                    stream,
                    "image/jpeg",
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.HotelImages,
                    newStorageKey,
                    CancellationToken.None),
            Times.Once);

        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    ImageContainer.HotelImages,
                    OldStorageKey,
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDeletingOldBlobFails_ShouldStillReturnSuccess()
    {
        // Arrange
        var hotel = CreateHotel();
        var image = CreateHotelImage();

        SetupOwner();

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        _imageStorageService
            .Setup(service =>
                service.DeleteAsync(
                    ImageContainer.HotelImages,
                    OldStorageKey,
                    CancellationToken.None))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Azure delete failure"));

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotEqual(
            OldStorageKey,
            image.StorageKey);

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentUserCannotManageHotel_ShouldReturnForbiddenWithoutTouchingStorage()
    {
        // Arrange
        var hotel = CreateHotel();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("another-owner");

        _currentUserService
            .Setup(service =>
                service.IsInRole(Roles.Admin))
            .Returns(false);

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

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

        _hotelImageRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenImageDoesNotBelongToHotel_ShouldReturnNotFoundWithoutUploading()
    {
        // Arrange
        var hotel = CreateHotel();

        var image =
            new HotelImage(
                hotelId: 999,
                storageKey: "999/image.jpg",
                displayOrder: 0,
                isPrimary: false);

        SetId(image, ImageId);

        SetupOwner();

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

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
            "HotelImage.NotFound",
            error.Code);

        _imageStorageService.VerifyNoOtherCalls();

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenImageSignatureIsInvalid_ShouldReturnValidationErrorWithoutUploading()
    {
        // Arrange
        var hotel = CreateHotel();
        var image = CreateHotelImage();

        SetupOwner();

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

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

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAdmin_ShouldAllowReplacementEvenWhenNotOwner()
    {
        // Arrange
        var hotel = CreateHotel();
        var image = CreateHotelImage();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-user");

        _currentUserService
            .Setup(service =>
                service.IsInRole(Roles.Admin))
            .Returns(true);

        _hotelRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    HotelId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelImageRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    ImageId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        var handler = CreateHandler();

        using var stream = CreateValidJpegStream();

        // Act
        var result =
            await handler.HandleAsync(
                CreateCommand(stream),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private UpdateHotelImageCommandHandler CreateHandler()
    {
        return new UpdateHotelImageCommandHandler(
            _validator,
            _hotelRepository.Object,
            _hotelImageRepository.Object,
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

    private static UpdateHotelImageCommand CreateCommand(
        Stream stream)
    {
        return new UpdateHotelImageCommand(
            HotelId,
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

        SetId(hotel, HotelId);

        return hotel;
    }

    private static HotelImage CreateHotelImage()
    {
        var image =
            new HotelImage(
                HotelId,
                OldStorageKey,
                displayOrder: 0,
                isPrimary: false);

        SetId(image, ImageId);

        return image;
    }

    private static void SetId<T>(
        T entity,
        int id)
    {
        var field =
            typeof(T).GetField(
                "<Id>k__BackingField",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

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