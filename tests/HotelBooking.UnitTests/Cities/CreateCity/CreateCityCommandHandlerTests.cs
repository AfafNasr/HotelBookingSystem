using FluentValidation;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.CreateCity;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Countries;
using HotelBooking.Domain.Cities;
using Moq;

namespace HotelBooking.UnitTests.Cities.CreateCity;

public sealed class CreateCityCommandHandlerTests
{
    private readonly Mock<ICountryRepository> _countryRepositoryMock = new();
    private readonly Mock<ICityRepository> _cityRepositoryMock = new();
    private readonly IValidator<CreateCityCommand> _validator =
        new CreateCityCommandValidator();

    [Fact]
    public async Task HandleAsync_WhenCountryDoesNotExist_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateCityCommand(
            "Amman",
            "ZZ",
            null);

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "ZZ",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CityId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("CountryNotFound", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _cityRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Domain.Cities.City>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private CreateCityCommandHandler CreateHandler()
    {
        return new CreateCityCommandHandler(
            _validator,
            _countryRepositoryMock.Object,
            _cityRepositoryMock.Object,
            TimeProvider.System);
    }

    [Fact]
    public async Task HandleAsync_WhenActiveCityAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var command = new CreateCityCommand(
            "Amman",
            "JO",
            null);

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var existingCity = new City(
            "Amman",
            "JO",
            null,
            DateTime.UtcNow);

        _cityRepositoryMock
            .Setup(repository => repository.GetByNameAndCountryAsync(
                "Amman",
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCity);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CityId);

        var error = Assert.Single(result.Errors);

        Assert.Equal("CityAlreadyExists", error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(
            "A city with the same name already exists in this country.",
            error.Description);

        _cityRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<City>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldCreateCity()
    {
        // Arrange
        var command = new CreateCityCommand(
            "  Irbid  ",
            " jo ",
            "  Irbid Central Post Office  ");

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cityRepositoryMock
            .Setup(repository => repository.GetByNameAndCountryAsync(
                "Irbid",
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        _cityRepositoryMock
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

        _cityRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<City>(city =>
                    city.Name == "Irbid" &&
                    city.CountryCode == "JO" &&
                    city.PostOffice == "Irbid Central Post Office" &&
                    !city.IsDeleted)),
            Times.Once);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingRepositories()
    {
        // Arrange
        var command = new CreateCityCommand(
            "",
            "J",
            null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CityId);
        Assert.NotEmpty(result.Errors);

        Assert.All(
            result.Errors,
            error => Assert.Equal(ErrorType.Validation, error.Type));

        _countryRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.GetByNameAndCountryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<City>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
