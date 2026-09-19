using FluentValidation.TestHelper;
using HotelBooking.Application.Cities.UpdateCity;

namespace HotelBooking.UnitTests.Cities.UpdateCity;

public sealed class UpdateCityValidatorTests
{
    private readonly UpdateCityValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateCityCommand(
            1,
            "Amman",
            "JO",
            "Amman Central Post Office");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCityIdIsInvalid_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            0,
            "Amman",
            "JO",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CityId);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            1,
            "",
            "JO",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            1,
            new string('A', 151),
            "JO",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenCountryCodeIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            1,
            "Amman",
            "",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Fact]
    public void Validate_WhenCountryCodeDoesNotHaveTwoCharacters_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            1,
            "Amman",
            "JOR",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Fact]
    public void Validate_WhenPostOfficeExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new UpdateCityCommand(
            1,
            "Amman",
            "JO",
            new string('A', 201));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PostOffice);
    }

    [Fact]
    public void Validate_WhenValuesContainSurroundingWhitespace_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateCityCommand(
            1,
            "  Amman  ",
            " JO ",
            "  Amman Central Post Office  ");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}