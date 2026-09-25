using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using Moq;

namespace HotelBooking.UnitTests.Users.PromoteToHotelOwner;

public sealed class PromoteToHotelOwnerHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly PromoteToHotelOwnerCommandHandler _handler;

    public PromoteToHotelOwnerHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();

        _handler = new PromoteToHotelOwnerCommandHandler(
            _identityServiceMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenPromotionSucceeds_ReturnsSuccessfulResult()
    {
        // Arrange
        const string userId = "user-123";

        var command = new PromoteToHotelOwnerCommand(userId);

        var expectedResult = new PromoteToHotelOwnerResult(
            true,
            Array.Empty<ApplicationError>());

        _identityServiceMock
            .Setup(service =>
                service.PromoteToHotelOwnerAsync(
                    userId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            service =>
                service.PromoteToHotelOwnerAsync(
                    userId,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenPromotionFails_ReturnsFailureFromIdentityService()
    {
        // Arrange
        const string userId = "missing-user";

        var expectedError = new ApplicationError(
            "User.NotFound",
            "The specified user was not found.",
            ErrorType.NotFound);

        var expectedResult = new PromoteToHotelOwnerResult(
            false,
            [expectedError]);

        _identityServiceMock
            .Setup(service =>
                service.PromoteToHotelOwnerAsync(
                    userId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new PromoteToHotelOwnerCommand(userId);

        // Act
        var result = await _handler.HandleAsync(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("User.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);

        _identityServiceMock.Verify(
            service =>
                service.PromoteToHotelOwnerAsync(
                    userId,
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
}