using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.CreateHotel;

public sealed class CreateHotelCommandHandlerTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IHotelRepository> _hotelRepositoryMock = new();

    private readonly IValidator<CreateHotelCommand> _validator =
        new CreateHotelCommandValidator();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingDependencies()
    {
        // Arrange
        var command = new CreateHotelCommand(
            "",
            0,
            "",
            6,
            (HotelCategory)999);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.HotelId);
        Assert.NotEmpty(result.Errors);

        Assert.All(
            result.Errors,
            error => Assert.Equal(ErrorType.Validation, error.Type));

        _cityRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _identityServiceMock.Verify(
            service => service.IsUserInRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Hotel>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCityDoesNotExist_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.HotelId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("CityNotFound", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _identityServiceMock.Verify(
            service => service.IsUserInRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Hotel>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotHotelOwner_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        var city = new City(
            "Nablus",
            "PS",
            null,
            DateTime.UtcNow);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _identityServiceMock
            .Setup(service => service.IsUserInRoleAsync(
                command.OwnerId,
                Roles.HotelOwner))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.HotelId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InvalidHotelOwner", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _hotelRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Hotel>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldCreateHotel()
    {
        // Arrange
        var command = CreateValidCommand();

        var city = new City(
            "Nablus",
            "PS",
            null,
            DateTime.UtcNow);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _identityServiceMock
            .Setup(service => service.IsUserInRoleAsync(
                command.OwnerId,
                Roles.HotelOwner))
            .ReturnsAsync(true);

        _hotelRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
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

        _hotelRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<Hotel>(hotel =>
                    hotel.Name == "Royal Nablus Hotel" &&
                    hotel.CityId == command.CityId &&
                    hotel.OwnerId == command.OwnerId &&
                    hotel.StarRating == 5 &&
                    hotel.Category == HotelCategory.Luxury &&
                    hotel.Description == null &&
                    hotel.Address == null &&
                    hotel.Latitude == null &&
                    hotel.Longitude == null &&
                    !hotel.IsDeleted)),
            Times.Once);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private CreateHotelCommandHandler CreateHandler()
    {
        return new CreateHotelCommandHandler(
            _validator,
            _cityRepositoryMock.Object,
            _identityServiceMock.Object,
            _hotelRepositoryMock.Object);
    }

    private static CreateHotelCommand CreateValidCommand()
    {
        return new CreateHotelCommand(
            "Royal Nablus Hotel",
            1,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury);
    }
}
