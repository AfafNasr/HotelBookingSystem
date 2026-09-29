using HotelBooking.Application.Amenities;
using HotelBooking.Application.Amenities.UpdateAmenity;
using HotelBooking.Domain.Amenities;
using Moq;

namespace HotelBooking.UnitTests.Amenities.UpdateAmenity;

public sealed class UpdateAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenityRepository = new();

    private readonly UpdateAmenityCommandValidator _validator = new();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private UpdateAmenityCommandHandler CreateHandler()
    {
        return new UpdateAmenityCommandHandler(
            _validator,
            _amenityRepository.Object,
            _timeProvider);
    }

    private static UpdateAmenityCommand CreateValidCommand()
    {
        return new UpdateAmenityCommand(
            AmenityId: 1,
            Name: "Swimming Pool");
    }

    private static Amenity CreateAmenity()
    {
        return new Amenity(
            name: "Free Wi-Fi",
            createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenAmenityIdIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            AmenityId = 0
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
            error => error.Code ==
                nameof(UpdateAmenityCommand.AmenityId));

        _amenityRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenNameIsEmpty()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Name = ""
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
            error => error.Code ==
                nameof(UpdateAmenityCommand.Name));

        _amenityRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenAmenityDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _amenityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Amenity?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Amenity.NotFound",
            error.Code);

        _amenityRepository.Verify(
            repository => repository.ExistsByNameExceptAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenAmenityNameAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();
        var amenity = CreateAmenity();

        _amenityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenity);

        _amenityRepository
            .Setup(repository => repository.ExistsByNameExceptAsync(
                command.Name,
                command.AmenityId,
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

        Assert.Equal(
            "Amenity.AlreadyExists",
            error.Code);

        Assert.Equal(
            "Free Wi-Fi",
            amenity.Name);

        Assert.Null(amenity.UpdatedAt);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateAmenity_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateValidCommand();
        var amenity = CreateAmenity();

        _amenityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenity);

        _amenityRepository
            .Setup(repository => repository.ExistsByNameExceptAsync(
                command.Name,
                command.AmenityId,
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

        Assert.Equal(
            command.Name,
            amenity.Name);

        Assert.NotNull(amenity.UpdatedAt);

        _amenityRepository.Verify(
            repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _amenityRepository.Verify(
            repository => repository.ExistsByNameExceptAsync(
                command.Name,
                command.AmenityId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}