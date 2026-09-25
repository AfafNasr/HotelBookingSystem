using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.UpdateHotel;
using HotelBooking.Domain.Cities;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.UpdateHotel;

public sealed class UpdateHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock = new();
    private readonly Mock<ICityRepository> _cityRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    private readonly IValidator<UpdateHotelCommand> _validator =
        new UpdateHotelCommandValidator();


    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingDependencies()
    {
        var command = new UpdateHotelCommand(
    0,
    "",
    0,
    "",
    6,
    (HotelCategory)999,
    new string('A', 2001),
    new string('A', 501),
    100m,
    200m);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        Assert.All(
            result.Errors,
            error => Assert.Equal(ErrorType.Validation, error.Type));

        _hotelRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

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
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHotelDoesNotExist_ShouldReturnNotFoundError()
    {
        var command = CreateValidCommand();

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("HotelNotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);

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
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCityDoesNotExist_ShouldReturnValidationError()
    {
        var command = CreateValidCommand();
        var hotel = CreateExistingHotel();

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.CityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("CityNotFound", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _identityServiceMock.Verify(
            service => service.IsUserInRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotHotelOwner_ShouldReturnValidationError()
    {
        var command = CreateValidCommand();
        var hotel = CreateExistingHotel();

        var city = new City(
            "Ramallah",
            "PS",
            null,
            DateTime.UtcNow);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

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

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("InvalidHotelOwner", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOptionalProfileDetailsAreNull_ShouldClearProfileDetails()
    {
        var hotel = CreateExistingHotel();

        hotel.CompleteProfile(
            "Existing description",
            "Existing address",
            32.2211m,
            35.2544m,
            DateTime.UtcNow.AddDays(-1));

        var command = CreateValidCommand() with
        {
            Description = null,
            Address = null,
            Latitude = null,
            Longitude = null
        };

        var city = new City(
            "Ramallah",
            "PS",
            null,
            DateTime.UtcNow);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

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

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldUpdateHotel()
    {
        var command = CreateValidCommand();
        var hotel = CreateExistingHotel();

        var city = new City(
            "Ramallah",
            "PS",
            null,
            DateTime.UtcNow);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

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

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal("Updated Hotel", hotel.Name);
        Assert.Equal(command.CityId, hotel.CityId);
        Assert.Equal(command.OwnerId, hotel.OwnerId);
        Assert.Equal(4, hotel.StarRating);
        Assert.Equal(HotelCategory.Boutique, hotel.Category);
        Assert.Equal(
    command.Description,
    hotel.Description);

        Assert.Equal(
            command.Address,
            hotel.Address);
        Assert.Equal(32.2211m, hotel.Latitude);
        Assert.Equal(35.2544m, hotel.Longitude);
        Assert.NotNull(hotel.UpdatedAt);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private UpdateHotelCommandHandler CreateHandler()
    {
        return new UpdateHotelCommandHandler(
            _validator,
            _hotelRepositoryMock.Object,
            _cityRepositoryMock.Object,
            _identityServiceMock.Object);
    }

    private static UpdateHotelCommand CreateValidCommand()
    {
        return new UpdateHotelCommand(
            1,
            "Updated Hotel",
            2,
            "new-hotel-owner-id",
            4,
            HotelCategory.Boutique,
            "Updated hotel description",
            "Updated hotel address",
            32.2211m,
            35.2544m);
    }

    private static Hotel CreateExistingHotel()
    {
        return new Hotel(
            "Original Hotel",
            1,
            "original-owner-id",
            5,
            HotelCategory.Luxury,
            DateTime.UtcNow);
    }
}
