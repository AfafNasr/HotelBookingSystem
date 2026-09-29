using FluentValidation.TestHelper;
using HotelBooking.Application.Reviews.UpdateReview;

namespace HotelBooking.UnitTests.Reviews.UpdateReview;

public sealed class UpdateReviewCommandValidatorTests
{
    private readonly UpdateReviewCommandValidator _validator = new();

    private static UpdateReviewCommand CreateValidCommand()
    {
        return new UpdateReviewCommand(
            ReviewId: 1,
            Rating: 5,
            Comment: "Updated review.");
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenReviewIdIsInvalid()
    {
        var command = CreateValidCommand() with
        {
            ReviewId = 0
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_ShouldHaveError_WhenRatingIsOutsideAllowedRange(
        int rating)
    {
        var command = CreateValidCommand() with
        {
            Rating = rating
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Validate_ShouldNotHaveRatingError_WhenRatingIsWithinAllowedRange(
        int rating)
    {
        var command = CreateValidCommand() with
        {
            Rating = rating
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCommentExceedsMaximumLength()
    {
        var command = CreateValidCommand() with
        {
            Comment = new string('A', 2001)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenCommentIsNull()
    {
        var command = CreateValidCommand() with
        {
            Comment = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }
}