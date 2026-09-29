using FluentValidation.TestHelper;
using HotelBooking.Application.Rooms.GetAdminRooms;

namespace HotelBooking.UnitTests.Rooms.GetAdminRooms;

public sealed class GetAdminRoomsQueryValidatorTests
{
    private readonly GetAdminRoomsQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsValid()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: "101");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldNotHaveErrors_WhenSearchIsNull()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: null);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenSearchExceedsMaximumLength()
    {
        // Arrange
        var query = new GetAdminRoomsQuery(
            Search: new string('A', 21));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Search);
    }
}