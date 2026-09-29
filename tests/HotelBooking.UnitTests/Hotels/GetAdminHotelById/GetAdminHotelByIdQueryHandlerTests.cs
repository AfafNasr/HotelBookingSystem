using HotelBooking.Application.Hotels.GetAdminHotelById;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.GetAdminHotelById;

public sealed class GetAdminHotelByIdQueryHandlerTests
{
    private readonly Mock<IAdminHotelByIdQuery> _hotelQuery = new();

    private GetAdminHotelByIdQueryHandler CreateHandler()
    {
        return new GetAdminHotelByIdQueryHandler(
            _hotelQuery.Object);
    }

    private static AdminHotelDetails CreateHotelDetails()
    {
        return new AdminHotelDetails(
            Id: 1,
            Name: "Test Hotel",
            CityId: 10,
            CityName: "Test City",
            OwnerId: "owner-1",
            OwnerName: "Test Owner",
            StarRating: 4,
            Category: HotelCategory.Luxury,
            Description: "Test hotel description.",
            Address: "Test Address",
            Latitude: 31.5m,
            Longitude: 35.1m,
            CreatedAt: new DateTime(
                2026,
                1,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc),
            UpdatedAt: null);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        const int hotelId = 1;

        _hotelQuery
            .Setup(query => query.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminHotelDetails?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            hotelId,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Hotel);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            error.Code);

        _hotelQuery.Verify(
            query => query.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHotel_WhenHotelExists()
    {
        // Arrange
        const int hotelId = 1;

        var hotel = CreateHotelDetails();

        _hotelQuery
            .Setup(query => query.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            hotelId,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(result.Hotel);
        Assert.Same(hotel, result.Hotel);

        _hotelQuery.Verify(
            query => query.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}