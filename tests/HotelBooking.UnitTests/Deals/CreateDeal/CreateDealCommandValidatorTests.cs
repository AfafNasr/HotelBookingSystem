using FluentValidation.TestHelper;
using HotelBooking.Application.Deals.CreateDeal;

namespace HotelBooking.UnitTests.Deals.CreateDeal;

public sealed class CreateDealCommandValidatorTests
{
    private readonly CreateDealCommandValidator _validator = new();

    private static CreateDealCommand CreateValidCommand()
    {
        return new CreateDealCommand(
            HotelId: 1,
            DiscountPercentage: 20m,
            StartDate: new DateOnly(2026, 10, 1),
            EndDate: new DateOnly(2026, 10, 10));
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            HotelId = 0
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDiscountPercentageIsZero()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = 0m
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.DiscountPercentage);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDiscountPercentageIsNegative()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = -1m
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.DiscountPercentage);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDiscountPercentageIsOneHundred()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = 100m
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.DiscountPercentage);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenDiscountPercentageIsGreaterThanOneHundred()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            DiscountPercentage = 101m
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.DiscountPercentage);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenStartDateEqualsEndDate()
    {
        // Arrange
        var date = new DateOnly(
            2026,
            10,
            1);

        var command = CreateValidCommand() with
        {
            StartDate = date,
            EndDate = date
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(
            command => command.EndDate);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            StartDate = new DateOnly(2026, 10, 10),
            EndDate = new DateOnly(2026, 10, 9)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.EndDate);
    }
}