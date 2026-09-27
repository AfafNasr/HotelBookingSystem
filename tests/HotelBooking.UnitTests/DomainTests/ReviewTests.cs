using HotelBooking.Domain.Reviews;

namespace HotelBooking.UnitTests.DomainTests;

public sealed class ReviewTests
{
    private static readonly DateTime CreatedAt =
        new(
            2026,
            9,
            27,
            10,
            0,
            0,
            DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateReview()
    {
        // Act
        var review =
            new Review(
                bookingId: 10,
                rating: 5,
                comment: "  Excellent stay  ",
                createdAt: CreatedAt);

        // Assert
        Assert.Equal(10, review.BookingId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Excellent stay", review.Comment);
        Assert.Equal(CreatedAt, review.CreatedAt);
        Assert.Null(review.UpdatedAt);
    }

    [Fact]
    public void Constructor_WhenCommentIsWhitespace_ShouldSetCommentToNull()
    {
        // Act
        var review =
            new Review(
                bookingId: 10,
                rating: 4,
                comment: "   ",
                createdAt: CreatedAt);

        // Assert
        Assert.Null(review.Comment);
    }

    [Fact]
    public void Constructor_WhenCommentIsNull_ShouldKeepCommentNull()
    {
        // Act
        var review =
            new Review(
                bookingId: 10,
                rating: 4,
                comment: null,
                createdAt: CreatedAt);

        // Assert
        Assert.Null(review.Comment);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateReview()
    {
        // Arrange
        var review =
            CreateReview();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        review.Update(
            rating: 3,
            comment: "  Good, but noisy  ",
            updatedAt: updatedAt);

        // Assert
        Assert.Equal(3, review.Rating);
        Assert.Equal("Good, but noisy", review.Comment);
        Assert.Equal(updatedAt, review.UpdatedAt);
    }

    [Fact]
    public void Update_WhenCommentIsWhitespace_ShouldSetCommentToNull()
    {
        // Arrange
        var review =
            CreateReview();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        review.Update(
            rating: 4,
            comment: "   ",
            updatedAt: updatedAt);

        // Assert
        Assert.Equal(4, review.Rating);
        Assert.Null(review.Comment);
        Assert.Equal(updatedAt, review.UpdatedAt);
    }

    [Fact]
    public void Update_WhenCommentIsNull_ShouldSetCommentToNull()
    {
        // Arrange
        var review =
            CreateReview();

        var updatedAt =
            CreatedAt.AddHours(1);

        // Act
        review.Update(
            rating: 2,
            comment: null,
            updatedAt: updatedAt);

        // Assert
        Assert.Equal(2, review.Rating);
        Assert.Null(review.Comment);
        Assert.Equal(updatedAt, review.UpdatedAt);
    }

    private static Review CreateReview()
    {
        return new Review(
            bookingId: 10,
            rating: 5,
            comment: "Excellent stay",
            createdAt: CreatedAt);
    }
}