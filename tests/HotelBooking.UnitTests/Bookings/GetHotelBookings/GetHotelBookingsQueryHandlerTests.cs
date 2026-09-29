using FluentValidation;
using HotelBooking.Application.Bookings.GetHotelBookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Hotels;
using HotelBooking.Domain.Bookings;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Bookings.GetHotelBookings;

public sealed class GetHotelBookingsQueryHandlerTests
{
    private readonly IValidator<GetHotelBookingsQuery> _validator;

    private readonly Mock<IHotelRepository> _hotelRepository;
    private readonly Mock<IHotelBookingsQuery> _hotelBookingsQuery;
    private readonly Mock<ICurrentUserService> _currentUserService;

    public GetHotelBookingsQueryHandlerTests()
    {
        _validator = new GetHotelBookingsQueryValidator();

        _hotelRepository = new Mock<IHotelRepository>();
        _hotelBookingsQuery = new Mock<IHotelBookingsQuery>();
        _currentUserService = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var query = new GetHotelBookingsQuery(
            HotelId: 0,
            Page: 1,
            PageSize: 20);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Bookings);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenPageIsInvalid()
    {
        // Arrange
        var query = new GetHotelBookingsQuery(
            HotelId: 1,
            Page: 0,
            PageSize: 20);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Bookings);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task HandleAsync_ShouldReturnValidationError_WhenPageSizeIsInvalid(
        int pageSize)
    {
        // Arrange
        var query = new GetHotelBookingsQuery(
            HotelId: 1,
            Page: 1,
            PageSize: pageSize);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Bookings);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenHotelDoesNotExist()
    {
        // Arrange
        var query = CreateValidQuery();

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
        Assert.Empty(result.Bookings);
        Assert.False(result.HasNextPage);
        Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            result.Errors.Single().Code);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenOwnerDoesNotOwnHotel()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-2");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Empty(result.Bookings);
        Assert.False(result.HasNextPage);
        Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.ManagementForbidden",
            result.Errors.Single().Code);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBookings_WhenOwnerOwnsHotel()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(false);

        IReadOnlyCollection<HotelBookingItem> bookings =
        [
            CreateBookingItem(
                bookingId: 10),

            CreateBookingItem(
                bookingId: 11)
        ];

        _hotelBookingsQuery
            .Setup(bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelBookingsPage(
                    bookings,
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(2, result.Bookings.Count);
        Assert.False(result.HasNextPage);

        Assert.Equal(
            query.Page,
            result.Page);

        Assert.Equal(
            query.PageSize,
            result.PageSize);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBookings_WhenCurrentUserIsAdmin()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(true);

        IReadOnlyCollection<HotelBookingItem> bookings =
        [
            CreateBookingItem(
                bookingId: 10)
        ];

        _hotelBookingsQuery
            .Setup(bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelBookingsPage(
                    bookings,
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Single(result.Bookings);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenHotelHasNoBookings()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(false);

        _hotelBookingsQuery
            .Setup(bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelBookingsPage(
                    Array.Empty<HotelBookingItem>(),
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Bookings);

        Assert.False(result.HasNextPage);

        Assert.Equal(
            query.Page,
            result.Page);

        Assert.Equal(
            query.PageSize,
            result.PageSize);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHasNextPage_WhenAnotherPageExists()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(false);

        IReadOnlyCollection<HotelBookingItem> bookings =
        [
            CreateBookingItem(
                bookingId: 10)
        ];

        _hotelBookingsQuery
            .Setup(bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelBookingsPage(
                    bookings,
                    true));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.HasNextPage);
        Assert.Single(result.Bookings);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassPaginationParametersToQuery()
    {
        // Arrange
        var query = new GetHotelBookingsQuery(
            HotelId: 1,
            Page: 3,
            PageSize: 10);

        var hotel = CreateHotel(
            ownerId: "owner-1");

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("owner-1");

        _currentUserService
            .Setup(service => service.IsInRole("Admin"))
            .Returns(false);

        _hotelBookingsQuery
            .Setup(bookingsQuery => bookingsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelBookingsPage(
                    Array.Empty<HotelBookingItem>(),
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _hotelBookingsQuery.Verify(
            bookingsQuery => bookingsQuery.GetAsync(
                1,
                3,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private GetHotelBookingsQueryHandler CreateHandler()
    {
        return new GetHotelBookingsQueryHandler(
            _validator,
            _hotelRepository.Object,
            _hotelBookingsQuery.Object,
            _currentUserService.Object);
    }

    private static GetHotelBookingsQuery CreateValidQuery()
    {
        return new GetHotelBookingsQuery(
            HotelId: 1,
            Page: 1,
            PageSize: 20);
    }

    private static Hotel CreateHotel(
        string ownerId)
    {
        return new Hotel(
            name: "Test Hotel",
            cityId: 1,
            ownerId: ownerId,
            starRating: 5,
            category: HotelCategory.Luxury,
            createdAt: new DateTime(
                2026,
                9,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc));
    }

    private static HotelBookingItem CreateBookingItem(
        int bookingId)
    {
        return new HotelBookingItem(
            BookingId: bookingId,
            GuestFullName: "Test Guest",
            GuestEmail: "guest@example.com",
            GuestPhoneNumber: "+970599000000",
            CheckInDate: new DateOnly(
                2026,
                10,
                10),
            CheckOutDate: new DateOnly(
                2026,
                10,
                12),
            NumberOfRooms: 1,
            TotalAmount: 300m,
            Status: BookingStatus.Confirmed,
            ConfirmationNumber: $"CONF-{bookingId}",
            CreatedAt: new DateTime(
                2026,
                9,
                20,
                10,
                0,
                0,
                DateTimeKind.Utc));
    }
}