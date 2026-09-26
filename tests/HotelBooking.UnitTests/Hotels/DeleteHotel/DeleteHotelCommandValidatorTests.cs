using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.DeleteHotel;

namespace HotelBooking.UnitTests.Hotels.DeleteHotel;

public sealed class DeleteHotelCommandValidatorTests
{
    private readonly DeleteHotelCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenHotelIdIsValid()
    {
        // Arrange
        var command = new DeleteHotelCommand(
            HotelId: 1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsZero()
    {
        // Arrange
        var command = new DeleteHotelCommand(
            HotelId: 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenHotelIdIsNegative()
    {
        // Arrange
        var command = new DeleteHotelCommand(
            HotelId: -1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }
}