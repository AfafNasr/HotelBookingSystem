using HotelBooking.Application.Authentication;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Users.CreateUser;
using Moq;

namespace HotelBooking.UnitTests.Users.CreateUser;

public sealed class CreateUserCommandHandlerTests
{
    private readonly Mock<IIdentityService>
        _identityService = new();

    private readonly CreateUserCommandValidator
        _validator = new();

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldCreateCustomer()
    {
        // Arrange
        var command =
            new CreateUserCommand(
                "new-customer",
                "customer@test.com",
                "Password123!");

        _identityService
            .Setup(service =>
                service.CreateCustomerAsync(
                    command.UserName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RegisterResult(
                    true,
                    "user-123",
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

        Assert.Equal(
            "user-123",
            result.UserId);

        _identityService.Verify(
            service =>
                service.CreateCustomerAsync(
                    command.UserName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserNameIsEmpty_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new CreateUserCommand(
                "",
                "customer@test.com",
                "Password123!");

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);
        Assert.NotEmpty(result.Errors);

        _identityService.Verify(
            service =>
                service.CreateCustomerAsync(
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
            new CreateUserCommand(
                "new-customer",
                "not-an-email",
                "Password123!");

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);
        Assert.NotEmpty(result.Errors);

        _identityService.Verify(
            service =>
                service.CreateCustomerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPasswordIsTooShort_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new CreateUserCommand(
                "new-customer",
                "customer@test.com",
                "123");

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);
        Assert.NotEmpty(result.Errors);

        _identityService.Verify(
            service =>
                service.CreateCustomerAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityServiceReturnsDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var command =
            new CreateUserCommand(
                "new-customer",
                "existing@test.com",
                "Password123!");

        var identityError =
            new ApplicationError(
                "DuplicateEmail",
                "Email is already taken.",
                ErrorType.Conflict);

        _identityService
            .Setup(service =>
                service.CreateCustomerAsync(
                    command.UserName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RegisterResult(
                    false,
                    null,
                    [identityError]));

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "DuplicateEmail",
            error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);

        _identityService.Verify(
            service =>
                service.CreateCustomerAsync(
                    command.UserName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityServiceReturnsDuplicateUserName_ShouldReturnFailure()
    {
        // Arrange
        var command =
            new CreateUserCommand(
                "existing-user",
                "new@test.com",
                "Password123!");

        var identityError =
            new ApplicationError(
                "DuplicateUserName",
                "Username is already taken.",
                ErrorType.Conflict);

        _identityService
            .Setup(service =>
                service.CreateCustomerAsync(
                    command.UserName,
                    command.Email,
                    command.Password,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RegisterResult(
                    false,
                    null,
                    [identityError]));

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.UserId);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "DuplicateUserName",
            error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);
    }

    private CreateUserCommandHandler CreateHandler()
    {
        return new CreateUserCommandHandler(
            _validator,
            _identityService.Object);
    }
}