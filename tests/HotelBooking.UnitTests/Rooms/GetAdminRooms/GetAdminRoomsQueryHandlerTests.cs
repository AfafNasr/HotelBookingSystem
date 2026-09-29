using HotelBooking.Application.Rooms.GetAdminRooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.GetAdminRooms;

public sealed class GetAdminRoomsQueryHandlerTests
{
    private readonly Mock<IAdminRoomsQuery> _adminRoomsQuery = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly GetAdminRoomsQueryValidator _validator = new();

    private GetAdminRoomsQueryHandler CreateHandler()
    {
        return new GetAdminRoomsQueryHandler(
            _validator,
            _adminRoomsQuery.Object,
            _timeProvider);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: new string('A', 21));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Rooms);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(GetAdminRoomsQuery.Search));

        _adminRoomsQuery.Verify(
            adminRoomsQuery => adminRoomsQuery.GetAsync(
                It.IsAny<GetAdminRoomsQuery>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRooms_WhenQueryIsValid()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: "101");

        IReadOnlyCollection<AdminRoom> expectedRooms =
        [
            new AdminRoom(
                Id: 1,
                RoomNumber: "101",
                IsAvailable: true,
                AdultsCapacity: 2,
                ChildrenCapacity: 1,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null),

            new AdminRoom(
                Id: 2,
                RoomNumber: "101-A",
                IsAvailable: false,
                AdultsCapacity: 3,
                ChildrenCapacity: 2,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: null)
        ];

        _adminRoomsQuery
            .Setup(adminRoomsQuery => adminRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
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

        _adminRoomsQuery.Verify(
            adminRoomsQuery => adminRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoRoomsAreFound()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: "999");

        IReadOnlyCollection<AdminRoom> emptyRooms =
            Array.Empty<AdminRoom>();

        _adminRoomsQuery
            .Setup(adminRoomsQuery => adminRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyRooms);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Rooms);

        _adminRoomsQuery.Verify(
            adminRoomsQuery => adminRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}