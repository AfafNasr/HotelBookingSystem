using FluentValidation;
using HotelBooking.Application.Amenities;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.HotelAmenities;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.HotelAmenities.AddHotelAmenity;

public sealed class AddHotelAmenityCommandHandlerTests
{
    private const string OwnerId = "owner-1";

    private readonly Mock<IHotelRepository> _hotelRepositoryMock = new();
    private readonly Mock<IAmenityRepository> _amenityRepositoryMock = new();
    private readonly Mock<IHotelAmenityRepository> _hotelAmenityRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly IValidator<AddHotelAmenityCommand> _validator =
        new AddHotelAmenityCommandValidator();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrorsWithoutAccessingRepositories()
    {
        var command = new AddHotelAmenityCommand(
            0,
            0);

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

        _amenityRepositoryMock.Verify(
            repository => repository.ExistsByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHotelDoesNotExist_ShouldReturnNotFoundError()
    {
        var command = new AddHotelAmenityCommand(
            1,
            1);

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

        _amenityRepositoryMock.Verify(
            repository => repository.ExistsByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<HotelAmenity>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenHotelBelongsToAnotherOwner_ShouldReturnAuthorizationError()
    {
        var command = new AddHotelAmenityCommand(
            1,
            1);

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
            "Hotel.ManagementForbidden",
            error.Code);

        Assert.Equal(
            ErrorType.Authorization,
            error.Type);

        // Security-sensitive ordering:
        // once ownership fails, we must not continue processing the amenity.
        _amenityRepositoryMock.Verify(
            repository => repository.ExistsByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<HotelAmenity>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAmenityDoesNotExist_ShouldReturnNotFoundError()
    {
        var command = new AddHotelAmenityCommand(
            1,
            999);

        var hotel = CreateHotel(OwnerId);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

        _amenityRepositoryMock
            .Setup(repository => repository.ExistsByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Amenity.NotFound",
            error.Code);

        Assert.Equal(
            ErrorType.NotFound,
            error.Type);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.ExistsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<HotelAmenity>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAmenityIsAlreadyAssigned_ShouldReturnConflictError()
    {
        var command = new AddHotelAmenityCommand(
            1,
            2);

        var hotel = CreateHotel(OwnerId);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

        _amenityRepositoryMock
            .Setup(repository => repository.ExistsByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _hotelAmenityRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                command.HotelId,
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "HotelAmenity.AlreadyExists",
            error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_ShouldAssignAmenityToHotel()
    {
        var command = new AddHotelAmenityCommand(
            1,
            2);

        var hotel = CreateHotel(OwnerId);

        _hotelRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(OwnerId);

        _amenityRepositoryMock
            .Setup(repository => repository.ExistsByIdAsync(
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _hotelAmenityRepositoryMock
            .Setup(repository => repository.ExistsAsync(
                command.HotelId,
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _hotelAmenityRepositoryMock
            .Setup(repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<HotelAmenity>(
                    hotelAmenity =>
                        hotelAmenity.HotelId == command.HotelId &&
                        hotelAmenity.AmenityId == command.AmenityId)),
            Times.Once);

        _hotelAmenityRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AddHotelAmenityCommandHandler CreateHandler()
    {
        return new AddHotelAmenityCommandHandler(
            _validator,
            _hotelRepositoryMock.Object,
            _amenityRepositoryMock.Object,
            _hotelAmenityRepositoryMock.Object,
            _currentUserServiceMock.Object);
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

