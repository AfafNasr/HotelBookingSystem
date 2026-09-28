using HotelBooking.Application.Carts.GetCart;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Carts.GetCart;

public sealed class GetCartQueryHandlerTests
{
    private readonly Mock<ICartQuery> _cartQuery = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private GetCartQueryHandler CreateHandler()
    {
        return new GetCartQueryHandler(
            _cartQuery.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Cart);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _cartQuery.Verify(
            query => query.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithNullCart_WhenUserHasNoCart()
    {
        // Arrange
        const string userId = "user-1";

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _cartQuery
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CartDetails?)null);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Null(result.Cart);

        _cartQuery.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCart_WhenUserHasCart()
    {
        // Arrange
        const string userId = "user-1";

        var cart =
            new CartDetails(
                CartId: 10,
                HotelId: 5,
                HotelName: "Test Hotel",
                Items:
                [
                    new CartItemDetails(
                        RoomId: 101,
                        RoomNumber: "A-101",
                        RoomType: RoomType.Deluxe,
                        Description: "Test room",
                        AdultsCapacity: 2,
                        ChildrenCapacity: 1,
                        PricePerNight: 150m,
                        IsRoomActive: true,
                        AddedAt: new DateTime(
                            2026,
                            9,
                            28,
                            8,
                            0,
                            0,
                            DateTimeKind.Utc))
                ]);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        _cartQuery
            .Setup(query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);

        Assert.NotNull(result.Cart);
        Assert.Same(cart, result.Cart);

        Assert.Equal(
            10,
            result.Cart.CartId);

        Assert.Equal(
            5,
            result.Cart.HotelId);

        Assert.Equal(
            "Test Hotel",
            result.Cart.HotelName);

        var item =
            Assert.Single(
                result.Cart.Items);

        Assert.Equal(
            101,
            item.RoomId);

        Assert.Equal(
            "A-101",
            item.RoomNumber);

        Assert.Equal(
            RoomType.Deluxe,
            item.RoomType);

        Assert.Equal(
            150m,
            item.PricePerNight);

        Assert.True(
            item.IsRoomActive);

        _cartQuery.Verify(
            query => query.GetAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}