using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.DeleteHotelImage;
using HotelBooking.Domain.Hotels;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBooking.UnitTests.Hotels.HotelImages;

public sealed class DeleteHotelImageCommandHandlerTests
{
    private const int HotelId = 10;
    private const int ImageId = 20;
    private const string OwnerId = "owner-1";
    private const string StorageKey = "10/image.jpg";

    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IHotelImageRepository> _hotelImageRepository = new();
    private readonly Mock<IImageStorageService> _imageStorageService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly Mock<ILogger<DeleteHotelImageCommandHandler>>
        _logger = new();

    private readonly DeleteHotelImageCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenOwnerDeletesImage_ShouldRemoveDatabaseRecordAndDeleteBlob()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _hotelImageRepository.Verify(
            repository =>
                repository.Remove(image),
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
                    StorageKey,
                    CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAzureDeleteFailsAfterDatabaseDelete_ShouldStillReturnSuccess()
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
                    StorageKey,
                    CancellationToken.None))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Azure failure"));

        var handler = CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _hotelImageRepository.Verify(
            repository =>
                repository.Remove(image),
            Times.Once);

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenDatabaseSaveFails_ShouldNotDeleteBlob()
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

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
                CancellationToken.None));

        // Assert
        _imageStorageService.Verify(
            service =>
                service.DeleteAsync(
                    It.IsAny<ImageContainer>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserCannotManageHotel_ShouldReturnForbiddenAndNotRemoveImage()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
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
                repository.Remove(
                    It.IsAny<HotelImage>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenImageDoesNotBelongToHotel_ShouldReturnNotFoundAndNotDeleteAnything()
    {
        // Arrange
        var hotel = CreateHotel();

        var image =
            new HotelImage(
                hotelId: 999,
                storageKey: "999/image.jpg",
                displayOrder: 0,
                isPrimary: false);

        SetId(
            image,
            ImageId);

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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "HotelImage.NotFound",
            error.Code);

        _hotelImageRepository.Verify(
            repository =>
                repository.Remove(
                    It.IsAny<HotelImage>()),
            Times.Never);

        _hotelImageRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _imageStorageService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsAdmin_ShouldAllowDeleteEvenWhenNotOwner()
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

        // Act
        var result =
            await handler.HandleAsync(
                new DeleteHotelImageCommand(
                    HotelId,
                    ImageId),
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _hotelImageRepository.Verify(
            repository =>
                repository.Remove(image),
            Times.Once);
    }

    private DeleteHotelImageCommandHandler CreateHandler()
    {
        return new DeleteHotelImageCommandHandler(
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

    private static HotelImage CreateHotelImage()
    {
        var image =
            new HotelImage(
                HotelId,
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