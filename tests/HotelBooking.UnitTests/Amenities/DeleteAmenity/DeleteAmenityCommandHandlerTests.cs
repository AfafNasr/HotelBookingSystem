using HotelBooking.Application.Amenities;
using HotelBooking.Application.Amenities.DeleteAmenity;
using HotelBooking.Domain.Amenities;
using Moq;

namespace HotelBooking.UnitTests.Amenities.DeleteAmenity;

public sealed class DeleteAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenityRepository = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private DeleteAmenityCommandHandler CreateHandler()
    {
        return new DeleteAmenityCommandHandler(
            _amenityRepository.Object,
            _timeProvider);
    }

    private static Amenity CreateAmenity()
    {
        return new Amenity(
            name: "Free Wi-Fi",
            createdAt: DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnInvalidId_WhenAmenityIdIsNotPositive()
    {
        // Arrange
        var command = new DeleteAmenityCommand(
            AmenityId: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Amenity.InvalidId",
            error.Code);

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
        var command = new DeleteAmenityCommand(
            AmenityId: 1);

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
            repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSoftDeleteAmenity_WhenAmenityExists()
    {
        // Arrange
        var command = new DeleteAmenityCommand(
            AmenityId: 1);

        var amenity = CreateAmenity();

        _amenityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(amenity);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.True(amenity.IsDeleted);
        Assert.NotNull(amenity.DeletedAt);
        Assert.NotNull(amenity.UpdatedAt);

        Assert.Equal(
            amenity.DeletedAt,
            amenity.UpdatedAt);

        _amenityRepository.Verify(
            repository => repository.GetByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _amenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}