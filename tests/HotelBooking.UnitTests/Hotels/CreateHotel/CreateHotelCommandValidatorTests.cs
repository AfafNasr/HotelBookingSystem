using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Domain.Hotels;

namespace HotelBooking.UnitTests.Hotels.CreateHotel;

public sealed class CreateHotelCommandValidatorTests
{
    private readonly CreateHotelCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new CreateHotelCommand(
            "Royal Nablus Hotel",
            1,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateHotelCommand(
            "",
            1,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveValidationError()
    {
        var command = new CreateHotelCommand(
            new string('A', 201),
            1,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Name);
    }

    [Fact]
    public void Validate_WhenCityIdIsNotPositive_ShouldHaveValidationError()
    {
        var command = new CreateHotelCommand(
            "Royal Nablus Hotel",
            0,
            "hotel-owner-id",
            5,
            HotelCategory.Luxury);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.CityId);
    }

    [Fact]
    public void Validate_WhenOwnerIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateHotelCommand(
            "Royal Nablus Hotel",
            1,
            "",
            5,
            HotelCategory.Luxury);

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
        var command = new CreateHotelCommand(
            "Royal Nablus Hotel",
            1,
            "hotel-owner-id",
            starRating,
            HotelCategory.Luxury);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.StarRating);
    }

    [Fact]
    public void Validate_WhenCategoryIsNotDefined_ShouldHaveValidationError()
    {
        var command = new CreateHotelCommand(
            "Royal Nablus Hotel",
            1,
            "hotel-owner-id",
            5,
            (HotelCategory)999);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(
            command => command.Category);
    }
}