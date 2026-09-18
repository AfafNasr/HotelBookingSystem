using HotelBooking.Application.Authentication.Register;

namespace HotelBooking.UnitTests.Authentication.Register;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WhenCommandIsValid_ShouldSucceed()
    {
        // Arrange
        var command = new RegisterCommand(
            "customer1",
            "customer1@example.com",
            "Customer123!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameIsEmpty_ShouldFail()
    {
        var command = new RegisterCommand(
            "",
            "customer1@example.com",
            "Customer123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RegisterCommand.Username));
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailIsEmpty_ShouldFail()
    {
        var command = new RegisterCommand(
            "customer1",
            "",
            "Customer123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailFormatIsInvalid_ShouldFail()
    {
        var command = new RegisterCommand(
            "customer1",
            "not-an-email",
            "Customer123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task ValidateAsync_WhenPasswordIsEmpty_ShouldFail()
    {
        var command = new RegisterCommand(
            "customer1",
            "customer1@example.com",
            "");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(RegisterCommand.Password));
    }
}