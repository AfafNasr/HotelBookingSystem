using HotelBooking.Application.Cities.GetAdminCities;
using Moq;

namespace HotelBooking.UnitTests.Cities.GetAdminCities;

public sealed class GetAdminCitiesQueryHandlerTests
{
    private readonly Mock<IAdminCitiesQuery> _adminCitiesQuery = new();
    private readonly GetAdminCitiesQueryValidator _validator = new();

    private GetAdminCitiesQueryHandler CreateHandler()
    {
        return new GetAdminCitiesQueryHandler(
            _validator,
            _adminCitiesQuery.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: new string('A', 201));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Cities);

        Assert.Contains(
            result.Errors,
            error => error.Code == nameof(GetAdminCitiesQuery.Search));

        _adminCitiesQuery.Verify(
            queryService => queryService.GetAsync(
                It.IsAny<GetAdminCitiesQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCities_WhenQueryIsValid()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: "Amman");

        IReadOnlyCollection<AdminCity> cities =
        [
            new AdminCity(
                Id: 1,
                Name: "Amman",
                Country: "Jordan",
                PostOffice: "11118",
                NumberOfHotels: 12,
                CreatedAt: new DateTime(
                    2026,
                    1,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                UpdatedAt: null),

            new AdminCity(
                Id: 2,
                Name: "Amman West",
                Country: "Jordan",
                PostOffice: null,
                NumberOfHotels: 5,
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

        _adminCitiesQuery
            .Setup(queryService => queryService.GetAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cities);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            2,
            result.Cities.Count);

        Assert.Same(
            cities,
            result.Cities);

        _adminCitiesQuery.Verify(
            queryService => queryService.GetAsync(
                query,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenNoCitiesMatch()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: "Unknown City");

        _adminCitiesQuery
            .Setup(queryService => queryService.GetAsync(
                query,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AdminCity>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Cities);
        Assert.Empty(result.Errors);

        _adminCitiesQuery.Verify(
            queryService => queryService.GetAsync(
                query,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}