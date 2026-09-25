using FluentValidation.TestHelper;
using HotelBooking.Application.Cities.CreateCity;

namespace HotelBooking.UnitTests.Cities.CreateCity;

public sealed class CreateCityValidatorTests
{
    private readonly CreateCityValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new CreateCityCommand(
            "Amman",
            "JO",
            "Amman Central Post Office");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateCityCommand(
            "",
            "JO",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new CreateCityCommand(
            new string('A', 151),
            "JO",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name);
    }

    [Fact]
    public void Validate_WhenCountryCodeIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateCityCommand(
            "Amman",
            "",
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.CountryCode);
    }

    [Theory]
    [InlineData("J")]
    [InlineData("JOR")]
    public void Validate_WhenCountryCodeLengthIsInvalid_ShouldHaveValidationError(
        string countryCode)
    {
        var command = new CreateCityCommand(
            "Amman",
            countryCode,
            null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.CountryCode);
    }

    [Fact]
    public void Validate_WhenPostOfficeExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new CreateCityCommand(
            "Amman",
            "JO",
            new string('A', 201));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.PostOffice);
    }
}
