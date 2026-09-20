using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.UpdateHotel;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.UnitTests.Hotels.UpdateHotel;

public sealed class UpdateHotelCommandValidatorTests
{
    private readonly UpdateHotelCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenHotelIdIsNotPositive_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { HotelId = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.HotelId);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { Name = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Name = new string('A', 201)
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_WhenCityIdIsNotPositive_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { CityId = 0 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.CityId);
    }

    [Fact]
    public void Validate_WhenOwnerIdIsEmpty_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with { OwnerId = "" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.OwnerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_WhenStarRatingIsOutsideAllowedRange_ShouldHaveValidationError(
        int starRating)
    {
        var command = CreateValidCommand() with
        {
            StarRating = starRating
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.StarRating);
    }

    [Fact]
    public void Validate_WhenCategoryIsNotDefined_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Category = (HotelCategory)999
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Category);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_WhenLatitudeIsOutsideAllowedRange_ShouldHaveValidationError(
        decimal latitude)
    {
        var command = CreateValidCommand() with
        {
            Latitude = latitude
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_WhenLongitudeIsOutsideAllowedRange_ShouldHaveValidationError(
        decimal longitude)
    {
        var command = CreateValidCommand() with
        {
            Longitude = longitude
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Longitude);
    }

    [Fact]
    public void Validate_WhenOnlyLatitudeIsProvided_ShouldHaveValidationError()
    {
        var command = CreateValidCommand() with
        {
            Longitude = null
        };

        var result = _validator.TestValidate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenLocationIsNull_ShouldNotHaveValidationErrors()
    {
        var command = CreateValidCommand() with
        {
            Latitude = null,
            Longitude = null
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static UpdateHotelCommand CreateValidCommand()
    {
        return new UpdateHotelCommand(
            1,
            "Royal Nablus Hotel",
            2,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury,
            32.2211m,
            35.2544m);
    }
}