using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Rooms.GetHotelRooms;
using HotelBooking.Domain.Hotels;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.GetHotelRooms;

public sealed class GetHotelRoomsQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();
    private readonly Mock<IHotelRoomsQuery> _hotelRoomsQuery = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private GetHotelRoomsQueryHandler CreateHandler()
    {
        return new GetHotelRoomsQueryHandler(
            _hotelRepository.Object,
            _hotelRoomsQuery.Object,
            _currentUserService.Object);
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

    private static IReadOnlyCollection<HotelRoom> CreateRooms()
    {
        return
        [
            new HotelRoom(
                Id: 1,
                RoomNumber: "101",
                RoomType: RoomType.Standard,
                Description: "Standard room",
                AdultsCapacity: 2,
                ChildrenCapacity: 1,
                PricePerNight: 100m),

            new HotelRoom(
                Id: 2,
                RoomNumber: "201",
                RoomType: RoomType.Deluxe,
                Description: "Deluxe room",
                AdultsCapacity: 3,
                ChildrenCapacity: 2,
                PricePerNight: 180m)
        ];
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var query = new GetHotelRoomsQuery(
            HotelId: 1);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Rooms);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.NotFound", error.Code);

        _hotelRoomsQuery.Verify(
            roomsQuery => roomsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorizationError_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var query = new GetHotelRoomsQuery(
            HotelId: 1);

        var hotel = CreateHotel(ownerId: "owner-2");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
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
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Rooms);

        var error = Assert.Single(result.Errors);

        Assert.Equal("Hotel.ManagementForbidden", error.Code);

        _hotelRoomsQuery.Verify(
            roomsQuery => roomsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRooms_WhenOwnerOwnsHotel()
    {
        // Arrange
        var query = new GetHotelRoomsQuery(
            HotelId: 1);

        var hotel = CreateHotel(ownerId: "owner-1");
        var expectedRooms = CreateRooms();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(false);

        _hotelRoomsQuery
            .Setup(roomsQuery => roomsQuery.GetAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRooms);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Equal(expectedRooms, result.Rooms);

        _hotelRoomsQuery.Verify(
            roomsQuery => roomsQuery.GetAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRooms_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var query = new GetHotelRoomsQuery(
            HotelId: 1);

        var hotel = CreateHotel(ownerId: "different-owner");
        var expectedRooms = CreateRooms();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole(Roles.Admin))
            .Returns(true);

        _hotelRoomsQuery
            .Setup(roomsQuery => roomsQuery.GetAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRooms);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Equal(expectedRooms, result.Rooms);

        _hotelRoomsQuery.Verify(
            roomsQuery => roomsQuery.GetAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}