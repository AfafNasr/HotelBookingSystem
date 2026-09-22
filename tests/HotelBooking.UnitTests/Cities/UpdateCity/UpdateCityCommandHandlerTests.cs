using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Countries;
using HotelBooking.Domain.Cities;
using Moq;

namespace HotelBooking.UnitTests.Cities.UpdateCity;

public sealed class UpdateCityCommandHandlerTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock = new();
    private readonly Mock<ICountryRepository> _countryRepositoryMock = new();
    private readonly UpdateCityValidator _validator = new();

    private UpdateCityCommandHandler CreateHandler()
    {
        return new UpdateCityCommandHandler(
            _cityRepositoryMock.Object,
            _countryRepositoryMock.Object,
            _validator);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationError()
    {
        var handler = CreateHandler();

        var command = new UpdateCityCommand(
            0,
            "",
            "JOR",
            null);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            error => error.Type == ErrorType.Validation);

        _cityRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _countryRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCityDoesNotExist_ShouldReturnNotFoundError()
    {
        var handler = CreateHandler();

        var command = new UpdateCityCommand(
            999,
            "Amman",
            "JO",
            null);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code == "CityNotFound" &&
                error.Type == ErrorType.NotFound);

        _countryRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCountryDoesNotExist_ShouldReturnValidationError()
    {
        var handler = CreateHandler();

        var city = new City(
            "Amman",
            "JO",
            null,
            DateTime.UtcNow);

        var command = new UpdateCityCommand(
            1,
            "Amman",
            "ZZ",
            null);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "ZZ",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code == "CountryNotFound" &&
                error.Type == ErrorType.Validation);

        _cityRepositoryMock.Verify(
            repository => repository.ExistsWithNameAndCountryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAnotherCityHasSameNameAndCountry_ShouldReturnConflictError()
    {
        var handler = CreateHandler();

        var city = new City(
            "Irbid",
            "JO",
            null,
            DateTime.UtcNow);

        var command = new UpdateCityCommand(
            2,
            "Amman",
            "JO",
            null);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cityRepositoryMock
    .Setup(repository => repository.ExistsWithNameAndCountryAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code == "CityAlreadyExists" &&
                error.Type == ErrorType.Conflict);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldUpdateCityAndSaveChanges()
    {
        var handler = CreateHandler();

        var createdAt = DateTime.UtcNow.AddDays(-1);

        var city = new City(
            "Old Name",
            "PS",
            "Old Post Office",
            createdAt);

        var command = new UpdateCityCommand(
            1,
            "  Amman  ",
            " jo ",
            "  Amman Central Post Office  ");

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _countryRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                "JO",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cityRepositoryMock
            .Setup(repository => repository.ExistsWithNameAndCountryAsync(
                "Amman",
                "JO",
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cityRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal("Amman", city.Name);
        Assert.Equal("JO", city.CountryCode);
        Assert.Equal(
            "Amman Central Post Office",
            city.PostOffice);

        Assert.Equal(createdAt, city.CreatedAt);
        Assert.NotNull(city.UpdatedAt);

        _cityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}