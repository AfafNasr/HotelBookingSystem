using FluentValidation.TestHelper;
using HotelBooking.Application.Amenities.CreateAmenity;

namespace HotelBooking.UnitTests.Amenities.CreateAmenity;

public sealed class CreateAmenityCommandValidatorTests
{
    private readonly CreateAmenityCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new CreateAmenityCommand("Swimming Pool");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateAmenityCommand("");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new CreateAmenityCommand(
            new string('A', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }
}
