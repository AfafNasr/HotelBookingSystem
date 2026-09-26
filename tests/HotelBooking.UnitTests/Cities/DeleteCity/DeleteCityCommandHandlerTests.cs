using FluentValidation;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.DeleteCity;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Domain.Cities;
using Moq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HotelBooking.UnitTests.Cities.DeleteCity;

public sealed class DeleteCityCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            26,
            10,
            30,
            0,
            TimeSpan.Zero);

    private readonly Mock<ICityRepository> _cityRepository;
    private readonly IValidator<DeleteCityCommand> _validator;

    public DeleteCityCommandHandlerTests()
    {
        _cityRepository = new Mock<ICityRepository>();
        _validator = new DeleteCityCommandValidator();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenCityIdIsInvalid()
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _cityRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepository.Verify(
            repository => repository.HasHotelsAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenCityDoesNotExist()
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: 1);

        _cityRepository
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
        Assert.Single(result.Errors);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
    "City.NotFound",
    error.Code);

        Assert.Equal(
            ErrorType.NotFound,
            error.Type);

        _cityRepository.Verify(
            repository => repository.HasHotelsAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _cityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenCityHasHotels()
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: 1);

        var city = CreateCity();

        _cityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _cityRepository
            .Setup(repository => repository.HasHotelsAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "City.HasHotels",
            error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);

        Assert.False(city.IsDeleted);
        Assert.Null(city.DeletedAt);

        _cityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSoftDeleteCity_WhenCityHasNoHotels()
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: 1);

        var city = CreateCity();

        _cityRepository
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(city);

        _cityRepository
            .Setup(repository => repository.HasHotelsAsync(
                command.CityId,
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

        Assert.True(city.IsDeleted);

        Assert.Equal(
            Now.UtcDateTime,
            city.DeletedAt);

        Assert.Equal(
            Now.UtcDateTime,
            city.UpdatedAt);

        _cityRepository.Verify(
            repository => repository.HasHotelsAsync(
                command.CityId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _cityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DeleteCityCommandHandler CreateHandler()
    {
        return new DeleteCityCommandHandler(
            _validator,
            _cityRepository.Object,
            new TestTimeProvider(Now));
    }

    private static City CreateCity()
    {
        return new City(
            name: "Bethlehem",
            countryCode: "PS",
            postOffice: "P100",
            createdAt: new DateTime(
                2026,
                9,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));
    }

    private sealed class TestTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}