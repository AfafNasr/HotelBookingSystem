using FluentValidation.TestHelper;
using HotelBooking.Application.Amenities.UpdateAmenity;

namespace HotelBooking.UnitTests.Amenities.UpdateAmenity;

public sealed class UpdateAmenityCommandValidatorTests
{
    private readonly UpdateAmenityCommandValidator _validator = new();

    private static UpdateAmenityCommand CreateValidCommand()
    {
        return new UpdateAmenityCommand(
            AmenityId: 1,
            Name: "Swimming Pool");
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
    public void Validate_ShouldHaveError_WhenAmenityIdIsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            AmenityId = 0
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.AmenityId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameIsEmpty()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Name = ""
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenNameExceedsMaximumLength()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Name = new string('A', 101)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenNameIsExactlyMaximumLength()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Name = new string('A', 100)
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(
            command => command.Name);
    }
}