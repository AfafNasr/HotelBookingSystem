using System.Reflection;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Deals;
using HotelBooking.Application.Deals.DeleteDeal;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Deals.DeleteDeal;

public sealed class DeleteDealCommandHandlerTests
{
    private readonly Mock<IDealRepository> _dealRepository;
    private readonly Mock<IHotelRepository> _hotelRepository;
    private readonly Mock<ICurrentUserService> _currentUserService;

    public DeleteDealCommandHandlerTests()
    {
        _dealRepository = new Mock<IDealRepository>();
        _hotelRepository = new Mock<IHotelRepository>();
        _currentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenDealIdIsInvalid()
    {
        // Arrange
        var command = new DeleteDealCommand(0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal(
            "Deal.InvalidId",
            result.Errors.Single().Code);

        _dealRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenDealDoesNotExist()
    {
        // Arrange
        var command = new DeleteDealCommand(1);

        _dealRepository
            .Setup(repository => repository.GetByIdAsync(
                command.DealId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal(
            "Deal.NotFound",
            result.Errors.Single().Code);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHotelNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = new DeleteDealCommand(1);

        var deal = CreateDeal(
            dealId: command.DealId,
            hotelId: 1);

        _dealRepository
            .Setup(repository => repository.GetByIdAsync(
                command.DealId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                deal.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal(
            "Hotel.NotFound",
            result.Errors.Single().Code);

        _dealRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenUserCannotManageHotel()
    {
        // Arrange
        var command = new DeleteDealCommand(1);

        const string ownerId = "owner-1";

        var deal = CreateDeal(
            dealId: command.DealId,
            hotelId: 1);

        var hotel = CreateHotel(
            hotelId: deal.HotelId,
            ownerId: ownerId);

        _dealRepository
            .Setup(repository => repository.GetByIdAsync(
                command.DealId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                deal.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("another-user");

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
        Assert.Single(result.Errors);
        Assert.Equal(
            "Hotel.ManagementForbidden",
            result.Errors.Single().Code);

        _dealRepository.Verify(
            repository => repository.Remove(
                It.IsAny<Deal>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteDeal_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = new DeleteDealCommand(1);

        const string ownerId = "owner-1";

        var deal = CreateDeal(
            dealId: command.DealId,
            hotelId: 1);

        var hotel = CreateHotel(
            hotelId: deal.HotelId,
            ownerId: ownerId);

        _dealRepository
            .Setup(repository => repository.GetByIdAsync(
                command.DealId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                deal.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(ownerId);

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _dealRepository.Verify(
            repository => repository.Remove(deal),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteDeal_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = new DeleteDealCommand(1);

        var deal = CreateDeal(
            dealId: command.DealId,
            hotelId: 1);

        var hotel = CreateHotel(
            hotelId: deal.HotelId,
            ownerId: "owner-1");

        _dealRepository
            .Setup(repository => repository.GetByIdAsync(
                command.DealId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                deal.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _dealRepository.Verify(
            repository => repository.Remove(deal),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DeleteDealCommandHandler CreateHandler()
    {
        return new DeleteDealCommandHandler(
            _dealRepository.Object,
            _hotelRepository.Object,
            _currentUserService.Object);
    }

    private static Deal CreateDeal(
        int dealId,
        int hotelId)
    {
        var deal = new Deal(
            hotelId,
            20m,
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 10),
            new DateTime(
                2026,
                9,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));

        SetId(deal, dealId);

        return deal;
    }

    private static Hotel CreateHotel(
        int hotelId,
        string ownerId)
    {
        var hotel = new Hotel(
            "Test Hotel",
            1,
            ownerId,
            4,
            HotelCategory.Luxury,
            new DateTime(
                2026,
                9,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));

        SetId(hotel, hotelId);

        return hotel;
    }

    private static void SetId<T>(
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

        property.SetValue(entity, id);
    }
}