using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.CompleteHotelProfile;

namespace HotelBooking.UnitTests.Hotels.CompleteHotelProfile;

public sealed class CompleteHotelProfileCommandValidatorTests
{
    private readonly CompleteHotelProfileCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = CreateValidCommand();

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenHotelIdIsNotPositive_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            HotelId = 0
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(
            x => x.HotelId);
    }

    [Fact]
    public async Task Validate_WhenDescriptionExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Description = new string('A', 2001)
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(
            x => x.Description);
    }

    [Fact]
    public async Task Validate_WhenAddressExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Address = new string('A', 501)
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(
            x => x.Address);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public async Task Validate_WhenLatitudeIsOutsideAllowedRange_ShouldHaveValidationError(
        decimal latitude)
    {
        var command = CreateValidCommand() with
        {
            Latitude = latitude
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(
            x => x.Latitude);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public async Task Validate_WhenLongitudeIsOutsideAllowedRange_ShouldHaveValidationError(
        decimal longitude)
    {
        var command = CreateValidCommand() with
        {
            Longitude = longitude
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(
            x => x.Longitude);
    }

    [Fact]
    public async Task Validate_WhenLatitudeIsProvidedWithoutLongitude_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Latitude = 32.2211m,
            Longitude = null
        };

        var result = await _validator.TestValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage ==
                "Latitude and longitude must either both be provided or both be null.");
    }

    [Fact]
    public async Task Validate_WhenLongitudeIsProvidedWithoutLatitude_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Latitude = null,
            Longitude = 35.2544m
        };

        var result = await _validator.TestValidateAsync(command);

        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage ==
                "Latitude and longitude must either both be provided or both be null.");
    }

    [Fact]
    public async Task Validate_WhenBothCoordinatesAreNull_ShouldNotHaveValidationErrors()
    {
        var command = CreateValidCommand() with
        {
            Latitude = null,
            Longitude = null
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public async Task Validate_WhenCoordinatesAreOnValidBoundaries_ShouldNotHaveValidationErrors(
        decimal latitude,
        decimal longitude)
    {
        var command = CreateValidCommand() with
        {
            Latitude = latitude,
            Longitude = longitude
        };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CompleteHotelProfileCommand CreateValidCommand()
    {
        return new CompleteHotelProfileCommand(
            1,
            "A modern hotel located near the city center.",
            "Rafidia, Nablus",
            32.2211m,
            35.2544m);
    }
}