using HotelBooking.Application.Hotels.GetAdminHotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.GetAdminHotels;

public sealed class GetAdminHotelsQueryHandlerTests
{
    private readonly Mock<IAdminHotelsQuery> _adminHotelsQuery = new();
    private readonly GetAdminHotelsQueryValidator _validator = new();

    private GetAdminHotelsQueryHandler CreateHandler()
    {
        return new GetAdminHotelsQueryHandler(
            _validator,
            _adminHotelsQuery.Object);
    }

    private static GetAdminHotelsQuery CreateValidQuery()
    {
        return new GetAdminHotelsQuery(
            Search: "Hotel");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminHotelsQuery(
            Search: new string('A', 201));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Hotels);

        Assert.Contains(
            result.Errors,
            error => error.Code ==
                nameof(GetAdminHotelsQuery.Search));

        _adminHotelsQuery.Verify(
            service => service.GetAsync(
                It.IsAny<GetAdminHotelsQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHotels_WhenQueryIsValid()
    {
        // Arrange
        var query = CreateValidQuery();

        IReadOnlyCollection<AdminHotel> hotels =
        [
            new AdminHotel(
                Id: 1,
                Name: "Grand Hotel",
                StarRating: 5,
                Owner: "Owner One",
                NumberOfRooms: 20,
                CreatedAt: new DateTime(
                    2026,
                    1,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                UpdatedAt: null),

            new AdminHotel(
                Id: 2,
                Name: "City Hotel",
                StarRating: 4,
                Owner: "Owner Two",
                NumberOfRooms: 10,
                CreatedAt: new DateTime(
                    2026,
                    2,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                UpdatedAt: null)
        ];

        _adminHotelsQuery
            .Setup(service => service.GetAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Same(hotels, result.Hotels);
        Assert.Equal(2, result.Hotels.Count);

        _adminHotelsQuery.Verify(
            service => service.GetAsync(
                query,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoHotelsExist()
    {
        // Arrange
        var query = CreateValidQuery();

        IReadOnlyCollection<AdminHotel> hotels =
            Array.Empty<AdminHotel>();

        _adminHotelsQuery
            .Setup(service => service.GetAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotels);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Hotels);

        _adminHotelsQuery.Verify(
            service => service.GetAsync(
                query,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}