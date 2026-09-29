using HotelBooking.Application.Authentication;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelBooking.UnitTests.Authentication.Register;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ShouldNotCreateCustomer()
    {
        var handler = new RegisterCommandHandler(
            _identityServiceMock.Object,
            _validator,
            NullLogger<RegisterCommandHandler>.Instance);

        var command = new RegisterCommand(
            "",
            "invalid-email",
            "");

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.False(result.Succeeded);

        Assert.All(
            result.Errors,
            error => Assert.Equal(
                ErrorType.Validation,
                error.Type));

        _identityServiceMock.Verify(
            service => service.CreateCustomerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandIsValid_ShouldCreateCustomer()
    {
        var expectedResult = new RegisterResult(
            true,
            "user-123",
            Array.Empty<ApplicationError>());

        _identityServiceMock
            .Setup(service => service.CreateCustomerAsync(
                "customer1",
                "customer1@example.com",
                "Customer123!",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = new RegisterCommandHandler(
            _identityServiceMock.Object,
            _validator,
            NullLogger<RegisterCommandHandler>.Instance);

        var command = new RegisterCommand(
            "customer1",
            "customer1@example.com",
            "Customer123!");

        var result = await handler.HandleAsync(
            command,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("user-123", result.UserId);

        _identityServiceMock.Verify(
            service => service.CreateCustomerAsync(
                "customer1",
                "customer1@example.com",
                "Customer123!",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}