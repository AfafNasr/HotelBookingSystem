using FluentValidation.TestHelper;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;

namespace HotelBooking.UnitTests.HotelAmenities.AddHotelAmenity;

public sealed class AddHotelAmenityCommandValidatorTests
{
    private readonly AddHotelAmenityCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new AddHotelAmenityCommand(
            1,
            1);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenHotelIdIsNotGreaterThanZero_ShouldHaveValidationError()
    {
        var command = new AddHotelAmenityCommand(
            0,
            1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }

    [Fact]
    public void Validate_WhenAmenityIdIsNotGreaterThanZero_ShouldHaveValidationError()
    {
        var command = new AddHotelAmenityCommand(
            1,
            0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.AmenityId);
    }
}