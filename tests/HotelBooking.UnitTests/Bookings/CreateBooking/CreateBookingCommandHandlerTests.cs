using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.CreateBooking;
using HotelBooking.Application.Bookings.Pricing;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Deals;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Deals;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelBooking.UnitTests.Bookings.CreateBooking;

public sealed class CreateBookingCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IDealRepository> _dealRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IBookingConcurrencyManager> _concurrencyManager = new();
    private readonly Mock<ILogger<CreateBookingCommandHandler>> _logger = new();
    private readonly BookingPricingCalculator _pricingCalculator = new();

    private readonly BookingOptions _bookingOptions = new()
    {
        PaymentHoldDurationMinutes = 15
    };

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly CreateBookingCommandValidator _validator;

    public CreateBookingCommandHandlerTests()
    {
        _validator = new CreateBookingCommandValidator(_timeProvider);
    }

    private CreateBookingCommandHandler CreateHandler()
    {
        return new CreateBookingCommandHandler(
            _validator,
            _hotelRepository.Object,
            _roomRepository.Object,
            _bookingRepository.Object,
            _dealRepository.Object,
            _currentUserService.Object,
            _pricingCalculator,
            _concurrencyManager.Object,
            _logger.Object,
            _timeProvider,
            _bookingOptions);
    }

    private static CreateBookingCommand CreateValidCommand()
    {
        return new CreateBookingCommand(
            HotelId: 1,
            RoomIds: [1, 2],
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 12),
            GuestFullName: "Test User",
            GuestEmail: "test@example.com",
            GuestPhoneNumber: "+970599123456",
            SpecialRequests: "Late check-in.");
    }

    private static Hotel CreateHotel()
    {
        return new Hotel(
            "Test Hotel",
            cityId: 1,
            ownerId: "owner-1",
            starRating: 4,
            HotelCategory.Luxury,
            DateTime.UtcNow);
    }

    private static Room CreateRoom(
        int id,
        int hotelId,
        string roomNumber,
        decimal pricePerNight)
    {
        var room = new Room(
            hotelId,
            roomNumber,
            RoomType.Standard,
            description: "Test room",
            adultsCapacity: 2,
            childrenCapacity: 1,
            pricePerNight,
            DateTime.UtcNow);

        typeof(Room)
            .GetProperty(nameof(Room.Id))!
            .SetValue(room, id);

        return room;
    }

    private void SetupConcurrencyManagerToExecuteOperation()
    {
        _concurrencyManager
            .Setup(manager => manager.ExecuteWithRoomLocksAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<Func<CancellationToken, Task<CreateBookingResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (
                    IReadOnlyCollection<int> _,
                    Func<CancellationToken, Task<CreateBookingResult>> operation,
                    CancellationToken cancellationToken
                ) => operation(cancellationToken));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenCommandIsInvalid()
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
        Assert.Null(result.BookingId);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(CreateBookingCommand.HotelId));

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command = CreateValidCommand();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.BookingId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

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
        Assert.Null(result.BookingId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _roomRepository.Verify(
            repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRoomNotFound_WhenNotAllRequestedRoomsExist()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel();

        IReadOnlyCollection<Room> rooms =
        [
            CreateRoom(
                id: 1,
                hotelId: command.HotelId,
                roomNumber: "101",
                pricePerNight: 100m)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _roomRepository
            .Setup(repository => repository.GetByIdsAsync(
                command.RoomIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.BookingId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.RoomNotFound",
            error.Code);

        _dealRepository.Verify(
            repository => repository.GetOverlappingDealsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _concurrencyManager.Verify(
            manager => manager.ExecuteWithRoomLocksAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<Func<CancellationToken, Task<CreateBookingResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRoomHotelMismatch_WhenRoomBelongsToAnotherHotel()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel();

        IReadOnlyCollection<Room> rooms =
        [
            CreateRoom(
                id: 1,
                hotelId: command.HotelId,
                roomNumber: "101",
                pricePerNight: 100m),

            CreateRoom(
                id: 2,
                hotelId: 999,
                roomNumber: "201",
                pricePerNight: 150m)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _roomRepository
            .Setup(repository => repository.GetByIdsAsync(
                command.RoomIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.BookingId);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Booking.RoomHotelMismatch",
            error.Code);

        _dealRepository.Verify(
            repository => repository.GetOverlappingDealsAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _concurrencyManager.Verify(
            manager => manager.ExecuteWithRoomLocksAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<Func<CancellationToken, Task<CreateBookingResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRoomIsUnavailableInsideLock()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel();

        IReadOnlyCollection<Room> rooms =
        [
            CreateRoom(
                id: 1,
                hotelId: command.HotelId,
                roomNumber: "101",
                pricePerNight: 100m),

            CreateRoom(
                id: 2,
                hotelId: command.HotelId,
                roomNumber: "102",
                pricePerNight: 150m)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _roomRepository
            .Setup(repository => repository.GetByIdsAsync(
                command.RoomIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        _dealRepository
            .Setup(repository => repository.GetOverlappingDealsAsync(
                command.HotelId,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Deal>());

        _bookingRepository
            .Setup(repository => repository.GetUnavailableRoomIdsAsync(
                command.RoomIds,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([2]);

        SetupConcurrencyManagerToExecuteOperation();

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.BookingId);
        Assert.Single(result.Errors);

        _concurrencyManager.Verify(
            manager => manager.ExecuteWithRoomLocksAsync(
                command.RoomIds,
                It.IsAny<Func<CancellationToken, Task<CreateBookingResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _bookingRepository.Verify(
            repository => repository.GetUnavailableRoomIdsAsync(
                command.RoomIds,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Never);

        _bookingRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateBooking_WhenRequestIsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        var hotel = CreateHotel();

        IReadOnlyCollection<Room> rooms =
        [
            CreateRoom(
                id: 1,
                hotelId: command.HotelId,
                roomNumber: "101",
                pricePerNight: 100m),

            CreateRoom(
                id: 2,
                hotelId: command.HotelId,
                roomNumber: "102",
                pricePerNight: 150m)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _roomRepository
            .Setup(repository => repository.GetByIdsAsync(
                command.RoomIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        _dealRepository
            .Setup(repository => repository.GetOverlappingDealsAsync(
                command.HotelId,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Deal>());

        _bookingRepository
            .Setup(repository => repository.GetUnavailableRoomIdsAsync(
                command.RoomIds,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        SetupConcurrencyManagerToExecuteOperation();

        Booking? createdBooking = null;

        _bookingRepository
            .Setup(repository => repository.Add(
                It.IsAny<Booking>()))
            .Callback<Booking>(booking =>
                createdBooking = booking);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdBooking);

        Assert.Equal(
            "user-1",
            createdBooking.UserId);

        Assert.Equal(
            command.HotelId,
            createdBooking.HotelId);

        Assert.Equal(
            command.CheckInDate,
            createdBooking.CheckInDate);

        Assert.Equal(
            command.CheckOutDate,
            createdBooking.CheckOutDate);

        Assert.Equal(
            BookingStatus.PendingPayment,
            createdBooking.Status);

        Assert.Equal(
            command.GuestFullName,
            createdBooking.GuestFullName);

        Assert.Equal(
            command.GuestEmail,
            createdBooking.GuestEmail);

        Assert.Equal(
            command.GuestPhoneNumber,
            createdBooking.GuestPhoneNumber);

        Assert.Equal(
            command.SpecialRequests,
            createdBooking.SpecialRequests);

        // Two nights:
        // Room 1: 100 * 2 = 200
        // Room 2: 150 * 2 = 300
        Assert.Equal(
            500m,
            createdBooking.TotalAmount);

        Assert.Equal(
            2,
            createdBooking.Rooms.Count);

        Assert.Contains(
            createdBooking.Rooms,
            bookingRoom =>
                bookingRoom.RoomId == 1 &&
                bookingRoom.OriginalPricePerNight == 100m);

        Assert.Contains(
            createdBooking.Rooms,
            bookingRoom =>
                bookingRoom.RoomId == 2 &&
                bookingRoom.OriginalPricePerNight == 150m);

        Assert.NotNull(createdBooking.ExpiresAt);

        var expectedExpiration =
            createdBooking.CreatedAt.AddMinutes(
                _bookingOptions.PaymentHoldDurationMinutes);

        Assert.Equal(
            expectedExpiration,
            createdBooking.ExpiresAt.Value);

        _concurrencyManager.Verify(
            manager => manager.ExecuteWithRoomLocksAsync(
                command.RoomIds,
                It.IsAny<Func<CancellationToken, Task<CreateBookingResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Once);

        _bookingRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyDeal_WhenCalculatingTotalAmount()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            RoomIds = [1]
        };

        var hotel = CreateHotel();

        IReadOnlyCollection<Room> rooms =
        [
            CreateRoom(
                id: 1,
                hotelId: command.HotelId,
                roomNumber: "101",
                pricePerNight: 100m)
        ];

        IReadOnlyCollection<Deal> deals =
        [
            new Deal(
                hotelId: command.HotelId,
                discountPercentage: 20m,
                startDate: command.CheckInDate,
                endDate: command.CheckOutDate.AddDays(-1),
                createdAt: DateTime.UtcNow)
        ];

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                command.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _roomRepository
            .Setup(repository => repository.GetByIdsAsync(
                command.RoomIds,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        _dealRepository
            .Setup(repository => repository.GetOverlappingDealsAsync(
                command.HotelId,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deals);

        _bookingRepository
            .Setup(repository => repository.GetUnavailableRoomIdsAsync(
                command.RoomIds,
                command.CheckInDate,
                command.CheckOutDate,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        SetupConcurrencyManagerToExecuteOperation();

        Booking? createdBooking = null;

        _bookingRepository
            .Setup(repository => repository.Add(
                It.IsAny<Booking>()))
            .Callback<Booking>(booking =>
                createdBooking = booking);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(createdBooking);

        // 2 nights * 100 = 200
        // 20% discount = 40
        // Final total = 160
        Assert.Equal(
            160m,
            createdBooking.TotalAmount);

        Assert.Single(createdBooking.Rooms);

        Assert.Contains(
            createdBooking.Rooms,
            bookingRoom =>
                bookingRoom.RoomId == 1 &&
                bookingRoom.OriginalPricePerNight == 100m);

        _bookingRepository.Verify(
            repository => repository.Add(
                It.IsAny<Booking>()),
            Times.Once);

        _bookingRepository.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}