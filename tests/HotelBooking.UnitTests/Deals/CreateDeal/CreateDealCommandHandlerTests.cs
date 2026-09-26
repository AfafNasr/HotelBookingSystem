using System.Reflection;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Deals;
using HotelBooking.Application.Deals.CreateDeal;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Deals.CreateDeal;

public sealed class CreateDealCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IDealRepository> _dealRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private readonly CreateDealCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private CreateDealCommandHandler CreateHandler()
    {
        return new CreateDealCommandHandler(
            _validator,
            _hotelRepository.Object,
            _dealRepository.Object,
            _currentUserService.Object,
            new TestTimeProvider(_now));
    }

    private static CreateDealCommand CreateValidCommand()
    {
        return new CreateDealCommand(
            HotelId: 1,
            DiscountPercentage: 20m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 10));
    }

    private static Hotel CreateHotel(
        int id = 1,
        string ownerId = "owner-1")
    {
        var hotel = new Hotel(
            name: "Test Hotel",
            cityId: 1,
            ownerId: ownerId,
            starRating: 4,
            category: HotelCategory.Luxury,
            createdAt: new DateTime(
                2026,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc));

        SetEntityId(
            hotel,
            id);

        return hotel;
    }

    private static void SetEntityId<T>(
        T entity,
        int id)
    {
        var property = typeof(T).GetProperty(
            "Id",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException(
                $"Entity {typeof(T).Name} does not contain an Id property.");
        }

        property.SetValue(
            entity,
            id);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenCommandIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = 0m
        };

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.DealId);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.Add(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
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
        Assert.Null(result.DealId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.Add(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel(
            id: command.HotelId,
            ownerId: "owner-2");

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
        Assert.Null(result.DealId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.ManagementForbidden",
            error.Code);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.Add(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenDealOverlapsExistingDeal()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel(
            id: command.HotelId,
            ownerId: "owner-1");

        Assert.Equal(
            command.HotelId,
            hotel.Id);

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.DealId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            DealErrors.OverlappingDeal.Code,
            error.Code);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.Add(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateDeal_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel(
            id: command.HotelId,
            ownerId: "owner-1");

        Assert.Equal(
            command.HotelId,
            hotel.Id);

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Deal? createdDeal = null;

        _dealRepository
            .Setup(repository => repository.Add(
                It.IsAny<Deal>()))
            .Callback<Deal>(deal =>
                createdDeal = deal);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdDeal);

        Assert.Equal(
            command.HotelId,
            createdDeal.HotelId);

        Assert.Equal(
            command.DiscountPercentage,
            createdDeal.DiscountPercentage);

        Assert.Equal(
            command.StartDate,
            createdDeal.StartDate);

        Assert.Equal(
            command.EndDate,
            createdDeal.EndDate);

        Assert.Equal(
            _now.UtcDateTime,
            createdDeal.CreatedAt);

        Assert.Equal(
            _now.UtcDateTime,
            createdDeal.UpdatedAt);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.Add(
                It.Is<Deal>(deal =>
                    deal.HotelId == command.HotelId &&
                    deal.DiscountPercentage ==
                        command.DiscountPercentage &&
                    deal.StartDate == command.StartDate &&
                    deal.EndDate == command.EndDate)),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateDeal_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel(
            id: command.HotelId,
            ownerId: "different-owner");

        Assert.Equal(
            command.HotelId,
            hotel.Id);

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Deal? createdDeal = null;

        _dealRepository
            .Setup(repository => repository.Add(
                It.IsAny<Deal>()))
            .Callback<Deal>(deal =>
                createdDeal = deal);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdDeal);

        Assert.Equal(
            command.HotelId,
            createdDeal.HotelId);

        Assert.Equal(
            command.DiscountPercentage,
            createdDeal.DiscountPercentage);

        Assert.Equal(
            command.StartDate,
            createdDeal.StartDate);

        Assert.Equal(
            command.EndDate,
            createdDeal.EndDate);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealAsync(
                command.HotelId,
                command.StartDate,
                command.EndDate,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.Add(
                It.Is<Deal>(deal =>
                    deal.HotelId == command.HotelId &&
                    deal.DiscountPercentage ==
                        command.DiscountPercentage &&
                    deal.StartDate == command.StartDate &&
                    deal.EndDate == command.EndDate)),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestTimeProvider : TimeProvider
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