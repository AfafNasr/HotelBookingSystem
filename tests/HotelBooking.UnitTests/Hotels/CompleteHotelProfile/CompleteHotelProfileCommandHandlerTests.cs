using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.CompleteHotelProfile;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.CompleteHotelProfile;

public sealed class CompleteHotelProfileCommandHandlerTests
{
    private const string OwnerId = "owner-1";

    private readonly Mock<IHotelRepository> _hotelRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly IValidator<CompleteHotelProfileCommand> _validator =
        new CompleteHotelProfileCommandValidator();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingRepository()
    {
        var command = new CompleteHotelProfileCommand(
            0,
            "Description",
            "Address",
            95m,
            200m);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        Assert.All(
            result.Errors,
            error => Assert.Equal(
                ErrorType.Validation,
                error.Type));

        _hotelRepositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
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

        Assert.Equal("Hotel.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHotelBelongsToAnotherOwner_ShouldReturnAuthorizationErrorWithoutSavingChanges()
    {
        var command = CreateValidCommand();

        var hotel = CreateHotel(
            ownerId: "different-owner");

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.Forbidden",
            error.Code);

        Assert.Equal(
            ErrorType.Authorization,
            error.Type);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        Assert.Null(hotel.Description);
        Assert.Null(hotel.Address);
        Assert.Null(hotel.Latitude);
        Assert.Null(hotel.Longitude);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_ShouldCompleteHotelProfileAndSaveChanges()
    {
        var command = CreateValidCommand();

        var hotel = CreateHotel(OwnerId);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

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

        Assert.Equal(
            command.Description,
            hotel.Description);

        Assert.Equal(
            command.Address,
            hotel.Address);

        Assert.Equal(
            command.Latitude,
            hotel.Latitude);

        Assert.Equal(
            command.Longitude,
            hotel.Longitude);

        Assert.NotNull(hotel.UpdatedAt);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileAlreadyHasValues_ShouldUpdateExistingProfile()
    {
        var hotel = CreateHotel(OwnerId);

        hotel.CompleteProfile(
            "Old description",
            "Old address",
            31.9000m,
            35.2000m,
            DateTime.UtcNow.AddDays(-1));

        var command = new CompleteHotelProfileCommand(
            1,
            "Updated description",
            "Updated address",
            32.2211m,
            35.2544m);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

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

        Assert.Equal(
            "Updated description",
            hotel.Description);

        Assert.Equal(
            "Updated address",
            hotel.Address);

        Assert.Equal(
            32.2211m,
            hotel.Latitude);

        Assert.Equal(
            35.2544m,
            hotel.Longitude);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenOptionalDetailsAreNull_ShouldClearProfileDetails()
    {
        var hotel = CreateHotel(OwnerId);

        hotel.CompleteProfile(
            "Existing description",
            "Existing address",
            32.2211m,
            35.2544m,
            DateTime.UtcNow.AddDays(-1));

        var command = new CompleteHotelProfileCommand(
            1,
            null,
            null,
            null,
            null);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

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

    private CompleteHotelProfileCommandHandler CreateHandler()
    {
        return new CompleteHotelProfileCommandHandler(
            _validator,
            _hotelRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    private static CompleteHotelProfileCommand CreateValidCommand()
    {
        return new CompleteHotelProfileCommand(
            1,
            "A modern hotel located near the city center.",
            "Rafidia, Nablus",
            32.2211m,
            35.2544m);
    }

    private static Hotel CreateHotel(string ownerId)
    {
        return new Hotel(
            "Test Hotel",
            1,
            ownerId,
            4,
            HotelCategory.Luxury,
            DateTime.UtcNow);
    }
}
