using HotelBooking.Application.Deals.UpdateDeal;

namespace HotelBooking.UnitTests.Deals.UpdateDeal;

public sealed class UpdateDealCommandValidatorTests
{
    private readonly UpdateDealCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceed_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_ShouldFail_WhenDealIdIsNotPositive(
        int dealId)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DealId = dealId
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(UpdateDealCommand.DealId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(101)]
    public async Task Validate_ShouldFail_WhenDiscountPercentageIsOutsideAllowedRange(
        decimal discountPercentage)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = discountPercentage
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(UpdateDealCommand.DiscountPercentage));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99)]
    public async Task Validate_ShouldSucceed_WhenDiscountPercentageIsWithinAllowedRange(
        decimal discountPercentage)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = discountPercentage
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            StartDate = new DateOnly(2026, 10, 10),
            EndDate = new DateOnly(2026, 10, 9)
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(UpdateDealCommand.EndDate));
    }

    [Fact]
    public async Task Validate_ShouldSucceed_WhenEndDateEqualsStartDate()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 10);

        var command = CreateValidCommand() with
        {
            StartDate = date,
            EndDate = date
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static UpdateDealCommand CreateValidCommand()
    {
        return new UpdateDealCommand(
            DealId: 1,
            DiscountPercentage: 20m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 10));
    }
}