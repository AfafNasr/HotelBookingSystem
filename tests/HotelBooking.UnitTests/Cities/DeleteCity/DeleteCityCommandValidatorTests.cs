using HotelBooking.Application.Cities.DeleteCity;

namespace HotelBooking.UnitTests.Cities.DeleteCity;

public sealed class DeleteCityCommandValidatorTests
{
    private readonly DeleteCityCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceed_WhenCityIdIsPositive()
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: 1);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_ShouldFail_WhenCityIdIsNotPositive(
        int cityId)
    {
        // Arrange
        var command = new DeleteCityCommand(
            CityId: cityId);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName ==
                nameof(DeleteCityCommand.CityId));
    }
}