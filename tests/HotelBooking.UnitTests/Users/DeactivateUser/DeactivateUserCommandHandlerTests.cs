using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Users.DeactivateUser;
using Moq;

namespace HotelBooking.UnitTests.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IIdentityService>
        _identityService = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly DeactivateUserCommandValidator
        _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            27,
            14,
            30,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldDeactivateUser()
    {
        // Arrange
        var command =
            new DeactivateUserCommand(
                "user-123");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        _identityService
            .Setup(service =>
                service.DeactivateUserAsync(
                    command.UserId,
                    _now.UtcDateTime,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DeactivateUserResult(
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
                service.DeactivateUserAsync(
                    "user-123",
                    _now.UtcDateTime,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsEmpty_ShouldReturnValidationErrorWithoutCallingIdentityService()
    {
        // Arrange
        var command =
            new DeactivateUserCommand(
                "");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

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
                service.DeactivateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAdminTriesToDeactivateSelf_ShouldReturnConflict()
    {
        // Arrange
        var command =
            new DeactivateUserCommand(
                "admin-1");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "User.CannotDeactivateSelf",
            error.Code);

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);

        _identityService.Verify(
            service =>
                service.DeactivateUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command =
            new DeactivateUserCommand(
                "missing-user");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        var error =
            new ApplicationError(
                "User.NotFound",
                "The specified user was not found.",
                ErrorType.NotFound);

        _identityService
            .Setup(service =>
                service.DeactivateUserAsync(
                    command.UserId,
                    _now.UtcDateTime,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DeactivateUserResult(
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
    public async Task HandleAsync_ShouldUseTimeProviderValueForDeactivationTimestamp()
    {
        // Arrange
        var command =
            new DeactivateUserCommand(
                "user-456");

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("admin-1");

        DateTime? capturedDeactivatedAt =
            null;

        _identityService
            .Setup(service =>
                service.DeactivateUserAsync(
                    command.UserId,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .Callback(
                (
                    string _,
                    DateTime deactivatedAt,
                    CancellationToken _) =>
                {
                    capturedDeactivatedAt =
                        deactivatedAt;
                })
            .ReturnsAsync(
                new DeactivateUserResult(
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

        Assert.Equal(
            _now.UtcDateTime,
            capturedDeactivatedAt);
    }

    private DeactivateUserCommandHandler CreateHandler()
    {
        return new DeactivateUserCommandHandler(
            _validator,
            _identityService.Object,
            _currentUserService.Object,
            new TestTimeProvider(_now));
    }

    private sealed class TestTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}