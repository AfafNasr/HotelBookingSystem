using HotelBooking.Application.Common.Interfaces;
using HotelBooking.Application.Common.Models;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using Moq;

namespace HotelBooking.UnitTests.Users.PromoteToHotelOwner;

public sealed class PromoteToHotelOwnerHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly PromoteToHotelOwnerHandler _handler;

    public PromoteToHotelOwnerHandlerTests()
    {
        _identityServiceMock = new Mock<IIdentityService>();

        _handler = new PromoteToHotelOwnerHandler(
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
                service.PromoteToHotelOwnerAsync(userId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        _identityServiceMock.Verify(
            service =>
                service.PromoteToHotelOwnerAsync(userId),
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
                service.PromoteToHotelOwnerAsync(userId))
            .ReturnsAsync(expectedResult);

        var command = new PromoteToHotelOwnerCommand(userId);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.Succeeded);

        var error = Assert.Single(result.Errors);

        Assert.Equal("User.NotFound", error.Code);
        Assert.Equal(ErrorType.NotFound, error.Type);

        _identityServiceMock.Verify(
            service =>
                service.PromoteToHotelOwnerAsync(userId),
            Times.Once);
    }
}