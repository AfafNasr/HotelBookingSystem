using HotelBooking.Application.Common.Security;
using HotelBooking.Application.HotelAmenities;
using HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.HotelAmenities.DeleteHotelAmenity;

public sealed class DeleteHotelAmenityCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IHotelAmenityRepository> _hotelAmenityRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly DeleteHotelAmenityCommandValidator _validator = new();

    private DeleteHotelAmenityCommandHandler CreateHandler()
    {
        return new DeleteHotelAmenityCommandHandler(
            _validator,
            _hotelRepository.Object,
            _hotelAmenityRepository.Object,
            _currentUserService.Object);
    }

    private static DeleteHotelAmenityCommand CreateValidCommand()
    {
        return new DeleteHotelAmenityCommand(
            HotelId: 1,
            AmenityId: 2);
    }

    private static Hotel CreateHotel(string ownerId = "owner-1")
    {
        return new Hotel(
            "Test Hotel",
            cityId: 1,
            ownerId,
            starRating: 4,
            HotelCategory.Luxury,
            DateTime.UtcNow);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            HotelId = 0
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
                nameof(DeleteHotelAmenityCommand.HotelId));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
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
                nameof(DeleteHotelAmenityCommand.AmenityId));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                It.IsAny<HotelAmenity>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _hotelAmenityRepository.Verify(
            repository => repository.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-2");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.ManagementForbidden",
            error.Code);

        _hotelAmenityRepository.Verify(
            repository => repository.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelAmenityDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _hotelAmenityRepository
            .Setup(repository => repository.GetAsync(
                command.HotelId,
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HotelAmenity?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "HotelAmenity.NotFound",
            error.Code);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                It.IsAny<HotelAmenity>()),
            Times.Never);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteHotelAmenity_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "owner-1");

        var hotelAmenity = new HotelAmenity(
            command.HotelId,
            command.AmenityId);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _hotelAmenityRepository
            .Setup(repository => repository.GetAsync(
                command.HotelId,
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotelAmenity);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                hotelAmenity),
            Times.Once);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteHotelAmenity_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel(ownerId: "different-owner");

        var hotelAmenity = new HotelAmenity(
            command.HotelId,
            command.AmenityId);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(true);

        _hotelAmenityRepository
            .Setup(repository => repository.GetAsync(
                command.HotelId,
                command.AmenityId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotelAmenity);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _hotelAmenityRepository.Verify(
            repository => repository.Remove(
                hotelAmenity),
            Times.Once);

        _hotelAmenityRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}