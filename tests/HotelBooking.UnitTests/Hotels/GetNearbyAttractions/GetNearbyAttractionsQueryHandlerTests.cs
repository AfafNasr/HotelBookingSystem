using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Hotels.GetNearbyAttractions;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Hotels.GetNearbyAttractions;

public sealed class GetNearbyAttractionsQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotelRepository = new();

    private readonly Mock<INearbyAttractionsService>
        _nearbyAttractionsService = new();

    private GetNearbyAttractionsQueryHandler CreateHandler()
    {
        return new GetNearbyAttractionsQueryHandler(
            new GetNearbyAttractionsQueryValidator(),
            _hotelRepository.Object,
            _nearbyAttractionsService.Object);
    }

    private static Hotel CreateHotelWithoutCoordinates()
    {
        return new Hotel(
            name: "Test Hotel",
            cityId: 10,
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

    private static Hotel CreateHotelWithCoordinates()
    {
        var hotel = CreateHotelWithoutCoordinates();

        hotel.CompleteProfile(
            description: "Test hotel description.",
            address: "Test Address",
            latitude: 32.221m,
            longitude: 35.254m,
            updatedAt: new DateTime(
                2026,
                1,
                2,
                10,
                0,
                0,
                DateTimeKind.Utc));

        return hotel;
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var query = new GetNearbyAttractionsQuery(
            HotelId: 0,
            RadiusMeters: 3000,
            Limit: 10);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Attractions);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenRadiusIsInvalid()
    {
        // Arrange
        var query = new GetNearbyAttractionsQuery(
            HotelId: 1,
            RadiusMeters: 50,
            Limit: 10);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Attractions);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenLimitIsInvalid()
    {
        // Arrange
        var query = new GetNearbyAttractionsQuery(
            HotelId: 1,
            RadiusMeters: 3000,
            Limit: 0);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Attractions);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        const int hotelId = 1;

        var query = new GetNearbyAttractionsQuery(
            HotelId: hotelId,
            RadiusMeters: 3000,
            Limit: 10);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Attractions);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            HotelErrors.NotFound.Code,
            error.Code);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCoordinatesNotConfigured_WhenHotelHasNoCoordinates()
    {
        // Arrange
        const int hotelId = 1;

        var hotel = CreateHotelWithoutCoordinates();

        var query = new GetNearbyAttractionsQuery(
            HotelId: hotelId,
            RadiusMeters: 3000,
            Limit: 10);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Attractions);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            HotelErrors.CoordinatesNotConfigured.Code,
            error.Code);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAttractions_WhenHotelHasCoordinates()
    {
        // Arrange
        const int hotelId = 1;
        const int radiusMeters = 3000;
        const int limit = 10;

        var hotel = CreateHotelWithCoordinates();

        var attractions = new[]
        {
            new NearbyAttraction(
                Name: "Clock Tower",
                Address: "Nablus",
                Latitude: 32.218845m,
                Longitude: 35.261747m,
                DistanceMeters: 736),

            new NearbyAttraction(
                Name: "Hammam",
                Address: "Nablus Old City",
                Latitude: 32.219273m,
                Longitude: 35.259878m,
                DistanceMeters: 555)
        };

        var query = new GetNearbyAttractionsQuery(
            HotelId: hotelId,
            RadiusMeters: radiusMeters,
            Limit: limit);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _nearbyAttractionsService
            .Setup(service => service.GetAsync(
                hotel.Latitude!.Value,
                hotel.Longitude!.Value,
                radiusMeters,
                limit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attractions);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(
            attractions.Length,
            result.Attractions.Count);

        Assert.Equal(
            attractions,
            result.Attractions);



        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()),
            Times.Once);



        Assert.True(
     hotel.Latitude.HasValue);

        Assert.True(
            hotel.Longitude.HasValue);

        var latitude =
            hotel.Latitude.GetValueOrDefault();

        var longitude =
            hotel.Longitude.GetValueOrDefault();

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                latitude,
                longitude,
                radiusMeters,
                limit,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCoordinatesRadiusAndLimitToService()
    {
        // Arrange
        const int hotelId = 1;
        const int radiusMeters = 5000;
        const int limit = 15;

        var hotel = CreateHotelWithCoordinates();

        var query = new GetNearbyAttractionsQuery(
            HotelId: hotelId,
            RadiusMeters: radiusMeters,
            Limit: limit);

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                hotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _nearbyAttractionsService
            .Setup(service => service.GetAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NearbyAttraction>());

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _nearbyAttractionsService.Verify(
            service => service.GetAsync(
                32.221m,
                35.254m,
                radiusMeters,
                limit,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}