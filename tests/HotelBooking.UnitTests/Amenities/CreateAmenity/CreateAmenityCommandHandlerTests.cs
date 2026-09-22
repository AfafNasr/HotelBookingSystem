using FluentValidation;
using HotelBooking.Application.Amenities;
using HotelBooking.Application.Amenities.CreateAmenity;
using HotelBooking.Application.Common.Models;
using Moq;

namespace HotelBooking.UnitTests.Amenities.CreateAmenity;

public sealed class CreateAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenityRepositoryMock = new();

    private readonly IValidator<CreateAmenityCommand> _validator =
        new CreateAmenityCommandValidator();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingRepository()
    {
        var command = new CreateAmenityCommand("");

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.AmenityId);
        Assert.NotEmpty(result.Errors);

        Assert.All(
            result.Errors,
            error => Assert.Equal(ErrorType.Validation, error.Type));

        _amenityRepositoryMock.Verify(
            repository => repository.ExistsByNameAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _amenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<Domain.Amenities.Amenity>()),
            Times.Never);

        _amenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAmenityAlreadyExists_ShouldReturnConflictError()
    {
        var command = new CreateAmenityCommand("Wi-Fi");

        _amenityRepositoryMock
            .Setup(repository => repository.ExistsByNameAsync(
                command.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.AmenityId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("AmenityAlreadyExists", error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);

        _amenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<Domain.Amenities.Amenity>()),
            Times.Never);

        _amenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldCreateAmenity()
    {
        var command = new CreateAmenityCommand("  Parking  ");

        _amenityRepositoryMock
            .Setup(repository => repository.ExistsByNameAsync(
                command.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _amenityRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _amenityRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<Domain.Amenities.Amenity>(
                    amenity =>
                        amenity.Name == "Parking" &&
                        !amenity.IsDeleted)),
            Times.Once);

        _amenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private CreateAmenityCommandHandler CreateHandler()
    {
        return new CreateAmenityCommandHandler(
            _validator,
            _amenityRepositoryMock.Object);
    }
}