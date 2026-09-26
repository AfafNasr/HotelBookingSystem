using FluentValidation.TestHelper;
using HotelBooking.Application.Hotels.GetAdminHotels;

namespace HotelBooking.UnitTests.Hotels.GetAdminHotels;

public sealed class GetAdminHotelsQueryValidatorTests
{
    private readonly GetAdminHotelsQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsValid()
    {
        // Arrange
        var query = new GetAdminHotelsQuery(
            Search: "Grand Hotel");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsNull()
    {
        // Arrange
        var query = new GetAdminHotelsQuery(
            Search: null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsExactlyMaximumLength()
    {
        // Arrange
        var query = new GetAdminHotelsQuery(
            Search: new string('A', 200));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminHotelsQuery(
            Search: new string('A', 201));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(
            query => query.Search);
    }
}