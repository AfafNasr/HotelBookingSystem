using FluentValidation;
using HotelBooking.Application.Hotels;
using HotelBooking.Application.Reviews.GetHotelReviews;
using HotelBooking.Domain.Hotels;
using Moq;

namespace HotelBooking.UnitTests.Reviews.GetHotelReviews;

public sealed class GetHotelReviewsQueryHandlerTests
{
    private readonly IValidator<GetHotelReviewsQuery> _validator;
    private readonly Mock<IHotelRepository> _hotelRepository;
    private readonly Mock<IHotelReviewsQuery> _hotelReviewsQuery;

    public GetHotelReviewsQueryHandlerTests()
    {
        _validator = new GetHotelReviewsQueryValidator();

        _hotelRepository = new Mock<IHotelRepository>();

        _hotelReviewsQuery = new Mock<IHotelReviewsQuery>();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var query = new GetHotelReviewsQuery(
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
        Assert.Empty(result.Reviews);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
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
        var query = new GetHotelReviewsQuery(
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
        Assert.Empty(result.Reviews);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
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
        var query = new GetHotelReviewsQuery(
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
        Assert.Empty(result.Reviews);
        Assert.NotEmpty(result.Errors);

        _hotelRepository.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
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
        Assert.Empty(result.Reviews);
        Assert.Single(result.Errors);

        Assert.Equal(
            "Hotel.NotFound",
            result.Errors.Single().Code);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyReviews_WhenHotelHasNoReviews()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelReviewsQuery
            .Setup(reviewsQuery => reviewsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelReviewsPage(
                    Array.Empty<HotelReviewItem>(),
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Reviews);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);
        Assert.False(result.HasNextPage);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnReviews_WhenHotelHasReviews()
    {
        // Arrange
        var query = CreateValidQuery();

        var hotel = CreateHotel();

        IReadOnlyCollection<HotelReviewItem> reviews =
        [
            new HotelReviewItem(
                1,
                5,
                "Excellent hotel.",
                new DateTime(
                    2026,
                    9,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                null),

            new HotelReviewItem(
                2,
                4,
                "Very good stay.",
                new DateTime(
                    2026,
                    9,
                    19,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    9,
                    21,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc))
        ];

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelReviewsQuery
            .Setup(reviewsQuery => reviewsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelReviewsPage(
                    reviews,
                    true));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.Equal(2, result.Reviews.Count);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);

        Assert.True(result.HasNextPage);

        var firstReview = result.Reviews.First();

        Assert.Equal(1, firstReview.ReviewId);
        Assert.Equal(5, firstReview.Rating);
        Assert.Equal(
            "Excellent hotel.",
            firstReview.Comment);

        var secondReview = result.Reviews.Skip(1).First();

        Assert.Equal(2, secondReview.ReviewId);
        Assert.Equal(4, secondReview.Rating);
        Assert.Equal(
            "Very good stay.",
            secondReview.Comment);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassPaginationParametersToQuery()
    {
        // Arrange
        var query = new GetHotelReviewsQuery(
            HotelId: 1,
            Page: 3,
            PageSize: 10);

        var hotel = CreateHotel();

        _hotelRepository
            .Setup(repository => repository.GetByIdAsync(
                query.HotelId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        _hotelReviewsQuery
            .Setup(reviewsQuery => reviewsQuery.GetAsync(
                query.HotelId,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HotelReviewsPage(
                    Array.Empty<HotelReviewItem>(),
                    false));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(
            query,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);

        _hotelReviewsQuery.Verify(
            reviewsQuery => reviewsQuery.GetAsync(
                1,
                3,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private GetHotelReviewsQueryHandler CreateHandler()
    {
        return new GetHotelReviewsQueryHandler(
            _validator,
            _hotelRepository.Object,
            _hotelReviewsQuery.Object);
    }

    private static GetHotelReviewsQuery CreateValidQuery()
    {
        return new GetHotelReviewsQuery(
            HotelId: 1,
            Page: 1,
            PageSize: 20);
    }

    private static Hotel CreateHotel()
    {
        return new Hotel(
            name: "Test Hotel",
            cityId: 1,
            ownerId: "owner-1",
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
}