using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.UploadHotelImage;
using HotelBooking.Domain.Hotels;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBooking.UnitTests.Hotels.UploadHotelImage;

public sealed class UploadHotelImageCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IHotelImageRepository> _hotelImageRepository = new();
    private readonly Mock<IImageStorageService> _imageStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<UploadHotelImageCommandHandler>> _logger = new();

    private readonly UploadHotelImageCommandValidator _validator = new();

    private UploadHotelImageCommandHandler CreateHandler()
    {
        return new UploadHotelImageCommandHandler(
            _validator,
            _hotelRepository.Object,
            _hotelImageRepository.Object,
            _imageStorageService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    private static UploadHotelImageCommand CreateValidCommand(
        Stream? content = null)
    {
        content ??= CreateValidPngStream();

        return new UploadHotelImageCommand(
            HotelId: 1,
            Content: content,
            FileName: "hotel.png",
            ContentType: "image/png",
            Length: content.Length);
    }

    private static Hotel CreateHotel(string ownerId = "owner-1")
    {
        return new Hotel(
            name: "Test Hotel",
            cityId: 1,
            ownerId: ownerId,
            starRating: 4,
            category: HotelCategory.Luxury,
            createdAt: DateTime.UtcNow);
    }

    private static MemoryStream CreateValidPngStream()
    {
        byte[] bytes =
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ];

        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateInvalidImageStream()
    {
        return new MemoryStream(
        [
            0x00, 0x01, 0x02, 0x03,
            0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B
        ]);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenCommandIsInvalid()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content) with
        {
            HotelId = 0
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
            error => error.Code ==
                nameof(UploadHotelImageCommand.HotelId));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.Verify(
            service => service.UploadAsync(
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
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);

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
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _imageStorageService.Verify(
            service => service.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelImageRepository.Verify(
            repository => repository.Add(
                It.IsAny<HotelImage>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);
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
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.ManagementForbidden",
            error.Code);

        _imageStorageService.Verify(
            service => service.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelImageRepository.Verify(
            repository => repository.Add(
                It.IsAny<HotelImage>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenImageSignatureIsInvalid()
    {
        // Arrange
        using var content = CreateInvalidImageStream();

        var command = CreateValidCommand(content);
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

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.ImageId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "InvalidImageContent",
            error.Code);

        _hotelImageRepository.Verify(
            repository => repository.GetNextDisplayOrderAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.Verify(
            service => service.UploadAsync(
                It.IsAny<ImageContainer>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelImageRepository.Verify(
            repository => repository.Add(
                It.IsAny<HotelImage>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUploadAndSaveImage_WhenOwnerOwnsHotel()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);
        var hotel = CreateHotel(ownerId: "owner-1");

        const int displayOrder = 3;

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

        _hotelImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                hotel.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(displayOrder);

        HotelImage? addedImage = null;

        _hotelImageRepository
            .Setup(repository => repository.Add(
                It.IsAny<HotelImage>()))
            .Callback<HotelImage>(image => addedImage = image);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(addedImage);

        Assert.Equal(hotel.Id, addedImage.HotelId);
        Assert.Equal(displayOrder, addedImage.DisplayOrder);
        Assert.False(addedImage.IsPrimary);

        Assert.StartsWith(
            $"{hotel.Id}/",
            addedImage.StorageKey);

        Assert.EndsWith(
            ".png",
            addedImage.StorageKey);

        _imageStorageService.Verify(
            service => service.UploadAsync(
                ImageContainer.HotelImages,
                It.Is<string>(key =>
                    key == addedImage.StorageKey),
                command.Content,
                command.ContentType,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _hotelImageRepository.Verify(
            repository => repository.Add(
                It.IsAny<HotelImage>()),
            Times.Once);

        _hotelImageRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUploadAndSaveImage_WhenCurrentUserIsAdmin()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);
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

        _hotelImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                hotel.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _imageStorageService.Verify(
            service => service.UploadAsync(
                ImageContainer.HotelImages,
                It.Is<string>(key =>
                    key.StartsWith($"{hotel.Id}/") &&
                    key.EndsWith(".png")),
                command.Content,
                command.ContentType,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _hotelImageRepository.Verify(
            repository => repository.Add(
                It.IsAny<HotelImage>()),
            Times.Once);

        _hotelImageRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteUploadedImageAndRethrow_WhenDatabaseSaveFails()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);
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

        _hotelImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                hotel.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _hotelImageRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Database save failed."));

        string? uploadedStorageKey = null;

        _imageStorageService
            .Setup(service => service.UploadAsync(
                ImageContainer.HotelImages,
                It.IsAny<string>(),
                command.Content,
                command.ContentType,
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
            service => service.DeleteAsync(
                ImageContainer.HotelImages,
                uploadedStorageKey,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrowOriginalException_WhenCleanupDeleteAlsoFails()
    {
        // Arrange
        using var content = CreateValidPngStream();

        var command = CreateValidCommand(content);
        var hotel = CreateHotel(ownerId: "owner-1");

        var databaseException =
            new InvalidOperationException(
                "Database save failed.");

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

        _hotelImageRepository
            .Setup(repository => repository.GetNextDisplayOrderAsync(
                hotel.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _hotelImageRepository
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(databaseException);

        _imageStorageService
            .Setup(service => service.DeleteAsync(
                ImageContainer.HotelImages,
                It.IsAny<string>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException(
                "Storage cleanup failed."));

        var handler = CreateHandler();

        // Act
        var thrownException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(
                    command,
                    CancellationToken.None));

        // Assert
        Assert.Same(
            databaseException,
            thrownException);

        _imageStorageService.Verify(
            service => service.DeleteAsync(
                ImageContainer.HotelImages,
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Once);
    }
}