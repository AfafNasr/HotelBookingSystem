using System.Reflection;
using FluentValidation;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Deals;
using HotelBooking.Application.Deals.UpdateDeal;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Deals.UpdateDeal;

public sealed class UpdateDealCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            15,
            10,
            30,
            0,
            TimeSpan.Zero);

    private readonly Mock<IDealRepository> _dealRepository;
    private readonly Mock<IHotelRepository> _hotelRepository;
    private readonly Mock<ICurrentUserService> _currentUserService;

    private readonly IValidator<UpdateDealCommand> _validator;

    public UpdateDealCommandHandlerTests()
    {
        _dealRepository = new Mock<IDealRepository>();
        _hotelRepository = new Mock<IHotelRepository>();
        _currentUserService = new Mock<ICurrentUserService>();

        _validator = new UpdateDealCommandValidator();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenDealIdIsInvalid()
    {
        // Arrange
        var command = new UpdateDealCommand(
            DealId: 0,
            DiscountPercentage: 20m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 10));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _dealRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenDiscountPercentageIsInvalid()
    {
        // Arrange
        var command = new UpdateDealCommand(
            DealId: 1,
            DiscountPercentage: 100m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 10));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _dealRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var command = new UpdateDealCommand(
            DealId: 1,
            DiscountPercentage: 20m,
            StartDate: new DateOnly(2026, 10, 10),
            EndDate: new DateOnly(2026, 10, 1));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _dealRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenDealDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

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
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHotelNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

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
            repository => repository.HasOverlappingDealExceptAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
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
        var command = CreateValidCommand();

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
            repository => repository.HasOverlappingDealExceptAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenUpdatedDatesOverlapAnotherDeal()
    {
        // Arrange
        var command = CreateValidCommand();

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealExceptAsync(
                deal.HotelId,
                command.StartDate,
                command.EndDate,
                deal.Id,
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

        Assert.Equal(
            "Deal.Overlapping",
            result.Errors.Single().Code);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateDeal_WhenOwnerOwnsHotel()
    {
        // Arrange
        var command = CreateValidCommand();

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealExceptAsync(
                deal.HotelId,
                command.StartDate,
                command.EndDate,
                deal.Id,
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

        Assert.Equal(
            command.DiscountPercentage,
            deal.DiscountPercentage);

        Assert.Equal(
            command.StartDate,
            deal.StartDate);

        Assert.Equal(
            command.EndDate,
            deal.EndDate);

        Assert.Equal(
            Now.UtcDateTime,
            deal.UpdatedAt);

        _dealRepository.Verify(
            repository => repository.HasOverlappingDealExceptAsync(
                deal.HotelId,
                command.StartDate,
                command.EndDate,
                deal.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateDeal_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var command = CreateValidCommand();

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

        _dealRepository
            .Setup(repository => repository.HasOverlappingDealExceptAsync(
                deal.HotelId,
                command.StartDate,
                command.EndDate,
                deal.Id,
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

        Assert.Equal(
            command.DiscountPercentage,
            deal.DiscountPercentage);

        Assert.Equal(
            command.StartDate,
            deal.StartDate);

        Assert.Equal(
            command.EndDate,
            deal.EndDate);

        Assert.Equal(
            Now.UtcDateTime,
            deal.UpdatedAt);

        _dealRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private UpdateDealCommandHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(Now);

        return new UpdateDealCommandHandler(
            _validator,
            _dealRepository.Object,
            _hotelRepository.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private static UpdateDealCommand CreateValidCommand()
    {
        return new UpdateDealCommand(
            DealId: 1,
            DiscountPercentage: 25m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 15));
    }

    private static Deal CreateDeal(
        int dealId,
        int hotelId)
    {
        var deal = new Deal(
            hotelId,
            10m,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 10),
            new DateTime(
                2026,
                9,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));

        SetId(
            deal,
            dealId);

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

        SetId(
            hotel,
            hotelId);

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

        property.SetValue(
            entity,
            id);
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