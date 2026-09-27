using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Users.UpdateUser;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelBooking.UnitTests.Users.UpdateUser;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly Mock<IIdentityService>
        _identityService = new();

    private readonly UpdateUserCommandValidator
        _validator = new();

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldUpdateUser()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "user-123",
                "updated-user",
                "updated@test.com");

        _identityService
            .Setup(service =>
                service.UpdateUserAsync(
                    command.UserId,
                    command.UserName,
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UpdateUserResult(
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
                service.UpdateUserAsync(
                    "user-123",
                    "updated-user",
                    "updated@test.com",
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsEmpty_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "",
                "updated-user",
                "updated@test.com");

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
                service.UpdateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNameIsEmpty_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "user-123",
                "",
                "updated@test.com");

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
                service.UpdateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsInvalid_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "user-123",
                "updated-user",
                "not-an-email");

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
                service.UpdateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "missing-user",
                "updated-user",
                "updated@test.com");

        var error =
            new ApplicationError(
                "User.NotFound",
                "The specified user was not found.",
                ErrorType.NotFound);

        _identityService
            .Setup(service =>
                service.UpdateUserAsync(
                    command.UserId,
                    command.UserName,
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UpdateUserResult(
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

        _identityService.Verify(
            service =>
                service.UpdateUserAsync(
                    command.UserId,
                    command.UserName,
                    command.Email,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "user-123",
                "updated-user",
                "existing@test.com");

        var error =
            new ApplicationError(
                "DuplicateEmail",
                "Email is already taken.",
                ErrorType.Conflict);

        _identityService
            .Setup(service =>
                service.UpdateUserAsync(
                    command.UserId,
                    command.UserName,
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UpdateUserResult(
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
            "DuplicateEmail",
            returnedError.Code);

        Assert.Equal(
            ErrorType.Conflict,
            returnedError.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNameAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var command =
            new UpdateUserCommand(
                "user-123",
                "existing-user",
                "updated@test.com");

        var error =
            new ApplicationError(
                "DuplicateUserName",
                "Username is already taken.",
                ErrorType.Conflict);

        _identityService
            .Setup(service =>
                service.UpdateUserAsync(
                    command.UserId,
                    command.UserName,
                    command.Email,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new UpdateUserResult(
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
            "DuplicateUserName",
            returnedError.Code);

        Assert.Equal(
            ErrorType.Conflict,
            returnedError.Type);
    }

    private UpdateUserCommandHandler CreateHandler()
    {
        return new UpdateUserCommandHandler(
            _validator,
            _identityService.Object,
              NullLogger<UpdateUserCommandHandler>.Instance);
    }
}