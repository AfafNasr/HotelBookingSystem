using FluentValidation.TestHelper;
using HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;

namespace HotelBooking.UnitTests.HotelAmenities.DeleteHotelAmenity;

public sealed class DeleteHotelAmenityCommandValidatorTests
{
    private readonly DeleteHotelAmenityCommandValidator _validator = new();

    private static DeleteHotelAmenityCommand CreateValidCommand()
    {
        return new DeleteHotelAmenityCommand(
            HotelId: 1,
            AmenityId: 2);
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
}