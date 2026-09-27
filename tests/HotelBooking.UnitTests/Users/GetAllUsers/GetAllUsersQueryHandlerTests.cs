using HotelBooking.Application.Authentication;
using HotelBooking.Application.Users.GetAllUsers;
using Moq;

namespace HotelBooking.UnitTests.Users.GetAllUsers;

public sealed class GetAllUsersQueryHandlerTests
{
    private readonly Mock<IIdentityService> _identityService = new();

    [Fact]
    public async Task HandleAsync_ShouldReturnUsersFromIdentityService()
    {
        // Arrange
        IReadOnlyCollection<AdminUserItem> users =
[
    new AdminUserItem(
        "user-1",
        "customer1",
        "customer1@test.com",
        ["Customer"],
        true,
        null),

    new AdminUserItem(
        "user-2",
        "owner1",
        "owner1@test.com",
        ["Customer", "HotelOwner"],
        false,
        new DateTime(
            2026,
            9,
            27,
            10,
            0,
            0,
            DateTimeKind.Utc))
];

        _identityService
            .Setup(service =>
                service.GetAllUsersAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var handler =
            new GetAllUsersQueryHandler(
                _identityService.Object);

        // Act
        var result =
            await handler.HandleAsync(
                CancellationToken.None);

        // Assert
        Assert.Equal(
            2,
            result.Count);

        var customer =
            result.Single(user =>
                user.Id == "user-1");

        Assert.Equal(
            "customer1",
            customer.UserName);

        Assert.Equal(
            "customer1@test.com",
            customer.Email);

        Assert.Single(
            customer.Roles);

        Assert.Contains(
            "Customer",
            customer.Roles);

        var owner =
            result.Single(user =>
                user.Id == "user-2");

        Assert.Equal(
            2,
            owner.Roles.Count);

        Assert.Contains(
            "Customer",
            owner.Roles);

        Assert.Contains(
            "HotelOwner",
            owner.Roles);
        Assert.True(customer.IsActive);
        Assert.Null(customer.DeactivatedAt);

        Assert.False(owner.IsActive);
        Assert.NotNull(owner.DeactivatedAt);

        _identityService.Verify(
            service =>
                service.GetAllUsersAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoUsersExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        _identityService
            .Setup(service =>
                service.GetAllUsersAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Array.Empty<AdminUserItem>());

        var handler =
            new GetAllUsersQueryHandler(
                _identityService.Object);

        // Act
        var result =
            await handler.HandleAsync(
                CancellationToken.None);

        // Assert
        Assert.Empty(result);

        _identityService.Verify(
            service =>
                service.GetAllUsersAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
}