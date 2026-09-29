using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Rooms.GetAvailableRooms;

public sealed class GetAvailableRoomsQueryHandlerTests
{
    private readonly Mock<IAvailableRoomsQuery> _availableRoomsQuery = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly GetAvailableRoomsQueryValidator _validator = new();

    private GetAvailableRoomsQueryHandler CreateHandler()
    {
        return new GetAvailableRoomsQueryHandler(
            _validator,
            _availableRoomsQuery.Object,
            _timeProvider);
    }

    private static GetAvailableRoomsQuery CreateValidQuery()
    {
        return new GetAvailableRoomsQuery(
            HotelId: 1,
            RoomType: RoomType.Standard,
            CheckInDate: new DateOnly(2026, 10, 10),
            CheckOutDate: new DateOnly(2026, 10, 15),
            Adults: 2,
            Children: 1);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenQueryIsInvalid()
    {
        // Arrange
        var query = CreateValidQuery() with
        {
            HotelId = 0
        };

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
            error => error.Code == nameof(GetAvailableRoomsQuery.HotelId));

        _availableRoomsQuery.Verify(
            availableRoomsQuery => availableRoomsQuery.GetAsync(
                It.IsAny<GetAvailableRoomsQuery>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoRoomsAreAvailable()
    {
        // Arrange
        var query = CreateValidQuery();

        _availableRoomsQuery
            .Setup(availableRoomsQuery => availableRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AvailableRoom>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Rooms);
        Assert.Empty(result.Errors);

        _availableRoomsQuery.Verify(
            availableRoomsQuery => availableRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassQueryToAvailableRoomsQuery_WhenQueryIsValid()
    {
        // Arrange
        var query = CreateValidQuery();

        _availableRoomsQuery
            .Setup(availableRoomsQuery => availableRoomsQuery.GetAsync(
                query,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AvailableRoom>());

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        _availableRoomsQuery.Verify(
            availableRoomsQuery => availableRoomsQuery.GetAsync(
                It.Is<GetAvailableRoomsQuery>(actual =>
                    actual.HotelId == query.HotelId &&
                    actual.RoomType == query.RoomType &&
                    actual.CheckInDate == query.CheckInDate &&
                    actual.CheckOutDate == query.CheckOutDate &&
                    actual.Adults == query.Adults &&
                    actual.Children == query.Children),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}