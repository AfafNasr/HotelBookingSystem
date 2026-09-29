using FluentValidation.TestHelper;
using HotelBooking.Application.Payments.StartPayment;

namespace HotelBooking.UnitTests.Payments.StartPayment;

public sealed class StartPaymentCommandValidatorTests
{
    private readonly StartPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenCommandIsValid()
    {
        // Arrange
        var command = new StartPaymentCommand(
            BookingId: 1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenBookingIdIsZero()
    {
        // Arrange
        var command = new StartPaymentCommand(
            BookingId: 0);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.BookingId);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenBookingIdIsNegative()
    {
        // Arrange
        var command = new StartPaymentCommand(
            BookingId: -1);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(
            command => command.BookingId);
    }
}