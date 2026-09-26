using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.DeleteHotel;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.DeleteHotel;

public sealed class DeleteHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly DeleteHotelCommandValidator _validator = new();

    private static readonly DateTimeOffset FixedUtcNow =
        new(
            2026,
            9,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

    private readonly TimeProvider _timeProvider =
        new FixedTimeProvider(FixedUtcNow);

    private DeleteHotelCommandHandler CreateHandler()
    {
        return new DeleteHotelCommandHandler(
            _validator,
            _hotelRepository.Object,
            _timeProvider);
    }

    private static DeleteHotelCommand CreateValidCommand()
    {
        return new DeleteHotelCommand(
            HotelId: 1);
    }

    private static Hotel CreateHotel()
    {
        return new Hotel(
            name: "Test Hotel",
            cityId: 1,
            ownerId: "owner-1",
            starRating: 4,
            category: HotelCategory.Luxury,
            createdAt: new DateTime(
                2026,
                1,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));
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
                nameof(DeleteHotelCommand.HotelId));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelRepository.Verify(
            repository => repository.HasActiveOrUpcomingBookingsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelRepository.Verify(
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

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _hotelRepository.Verify(
            repository => repository.HasActiveOrUpcomingBookingsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenHotelHasActiveOrUpcomingBookings()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel();

        var expectedNow = FixedUtcNow.UtcDateTime;
        var expectedToday =
            DateOnly.FromDateTime(expectedNow);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelRepository
            .Setup(repository => repository.HasActiveOrUpcomingBookingsAsync(
                hotel.Id,
                expectedToday,
                expectedNow,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.HasActiveBookings",
            error.Code);

        Assert.False(hotel.IsDeleted);
        Assert.Null(hotel.DeletedAt);
        Assert.Null(hotel.UpdatedAt);

        _hotelRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldSoftDeleteHotel_WhenHotelHasNoActiveOrUpcomingBookings()
    {
        // Arrange
        var command = CreateValidCommand();
        var hotel = CreateHotel();

        var expectedNow = FixedUtcNow.UtcDateTime;
        var expectedToday =
            DateOnly.FromDateTime(expectedNow);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelRepository
            .Setup(repository => repository.HasActiveOrUpcomingBookingsAsync(
                hotel.Id,
                expectedToday,
                expectedNow,
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

        Assert.True(hotel.IsDeleted);

        Assert.Equal(
            expectedNow,
            hotel.DeletedAt);

        Assert.Equal(
            expectedNow,
            hotel.UpdatedAt);

        _hotelRepository.Verify(
            repository => repository.HasActiveOrUpcomingBookingsAsync(
                hotel.Id,
                expectedToday,
                expectedNow,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _hotelRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}