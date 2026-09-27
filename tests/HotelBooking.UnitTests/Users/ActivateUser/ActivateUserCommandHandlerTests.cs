using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Users.ActivateUser;
using HotelBooking.Application.Users.CreateUser;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelBooking.UnitTests.Users.ActivateUser;

public sealed class ActivateUserCommandHandlerTests
{
    private readonly Mock<IIdentityService>
        _identityService = new();

    private readonly ActivateUserCommandValidator
        _validator = new();

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldActivateUser()
    {
        // Arrange
        var command =
            new ActivateUserCommand(
                "user-123");

        _identityService
            .Setup(service =>
                service.ActivateUserAsync(
                    command.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ActivateUserResult(
                    true,
                    Array.Empty<ApplicationError>()));

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _identityService.Verify(
            service =>
                service.ActivateUserAsync(
                    "user-123",
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsEmpty_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new ActivateUserCommand(
                "");

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);

        _identityService.Verify(
            service =>
                service.ActivateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command =
            new ActivateUserCommand(
                "missing-user");

        var error =
            new ApplicationError(
                "User.NotFound",
                "The specified user was not found.",
                ErrorType.NotFound);

        _identityService
            .Setup(service =>
                service.ActivateUserAsync(
                    command.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ActivateUserResult(
                    false,
                    [error]));

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var returnedError =
            Assert.Single(result.Errors);

        Assert.Equal(
            "User.NotFound",
            returnedError.Code);

        Assert.Equal(
            ErrorType.NotFound,
            returnedError.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityServiceReturnsSuccess_ShouldReturnSuccessUnchanged()
    {
        // Arrange
        var command =
            new ActivateUserCommand(
                "inactive-user");

        _identityService
            .Setup(service =>
                service.ActivateUserAsync(
                    command.UserId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ActivateUserResult(
                    true,
                    []));

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _identityService.Verify(
            service =>
                service.ActivateUserAsync(
                    command.UserId,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private ActivateUserCommandHandler CreateHandler()
    {
        return new ActivateUserCommandHandler(
    _validator,
    _identityService.Object,
    NullLogger<ActivateUserCommandHandler>.Instance);
    }
}