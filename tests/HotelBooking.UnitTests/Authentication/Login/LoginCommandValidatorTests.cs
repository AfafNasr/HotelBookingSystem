using HotelBooking.Application.Authentication.Login;

namespace HotelBooking.UnitTests.Authentication.Login;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = new LoginCommand(
            "customer1",
            "Password123!");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WhenUsernameIsEmpty_ShouldFail()
    {
        var command = new LoginCommand(
            "",
            "Password123!");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(LoginCommand.Username));
    }

    [Fact]
    public async Task Validate_WhenPasswordIsEmpty_ShouldFail()
    {
        var command = new LoginCommand(
            "customer1",
            "");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(LoginCommand.Password));
    }
}