using FluentValidation.TestHelper;
using HotelBooking.Application.Cities.GetAdminCities;

namespace HotelBooking.UnitTests.Cities.GetAdminCities;

public sealed class GetAdminCitiesQueryValidatorTests
{
    private readonly GetAdminCitiesQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsNull()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsValid()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: "Amman");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenSearchIsExactlyMaximumLength()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: new string('A', 200));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveValidationErrorFor(
            query => query.Search);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminCitiesQuery(
            Search: new string('A', 201));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(
            query => query.Search);
    }
}