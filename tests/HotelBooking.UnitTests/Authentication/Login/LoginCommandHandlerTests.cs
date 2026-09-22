using HotelBooking.Application.Authentication;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Common.Models;
using Moq;

namespace HotelBooking.UnitTests.Authentication.Login;

public sealed class LoginHandlerTests
{
    private readonly LoginCommandValidator _validator = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();

    private LoginCommandHandler CreateHandler()
    {
        return new LoginCommandHandler(
            _validator,
            _identityServiceMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldReturnValidationErrors()
    {
        var handler = CreateHandler();
        var command = new LoginCommand("", "");

        var result = await handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Null(result.AccessToken);
        Assert.Contains(
            result.Errors,
            error => error.Type == ErrorType.Validation);

        _identityServiceMock.Verify(
            service => service.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _tokenServiceMock.Verify(
            service => service.CreateToken(
                It.IsAny<AuthenticatedUser>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsAreInvalid_ShouldReturnAuthenticationError()
    {
        var handler = CreateHandler();
        var command = new LoginCommand(
            "customer1",
            "WrongPassword123!");

        _identityServiceMock
            .Setup(service => service.AuthenticateAsync(
                command.Username,
                command.Password))
            .ReturnsAsync((AuthenticatedUser?)null);

        var result = await handler.HandleAsync(command);

        Assert.False(result.Succeeded);
        Assert.Null(result.AccessToken);

        Assert.Contains(
            result.Errors,
            error => error.Type == ErrorType.Authentication);

        _identityServiceMock.Verify(
            service => service.AuthenticateAsync(
                command.Username,
                command.Password),
            Times.Once);

        _tokenServiceMock.Verify(
            service => service.CreateToken(
                It.IsAny<AuthenticatedUser>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsAreValid_ShouldReturnAccessToken()
    {
        var handler = CreateHandler();

        var command = new LoginCommand(
            "customer1",
            "Password123!");

        var authenticatedUser = new AuthenticatedUser(
            "user-123",
            "customer1",
            new[] { "Customer" });

        var accessToken = new AccessToken(
            "test-access-token",
            DateTimeOffset.UtcNow.AddMinutes(60));

        _identityServiceMock
            .Setup(service => service.AuthenticateAsync(
                command.Username,
                command.Password))
            .ReturnsAsync(authenticatedUser);

        _tokenServiceMock
            .Setup(service => service.CreateToken(authenticatedUser))
            .Returns(accessToken);

        var result = await handler.HandleAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            service => service.AuthenticateAsync(
                command.Username,
                command.Password),
            Times.Once);

        _tokenServiceMock.Verify(
            service => service.CreateToken(authenticatedUser),
            Times.Once);
    }
}