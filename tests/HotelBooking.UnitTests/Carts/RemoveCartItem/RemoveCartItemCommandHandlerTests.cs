using System.Reflection;
using HotelBooking.Application.Carts;
using HotelBooking.Application.Carts.RemoveCartItem;
using HotelBooking.Application.Common.Security;
using HotelBooking.Domain.Carts;
using Moq;

namespace HotelBooking.UnitTests.Carts.RemoveCartItem;

public sealed class RemoveCartItemCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly RemoveCartItemCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            28,
            10,
            0,
            0,
            TimeSpan.Zero);

    private RemoveCartItemCommandHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(_now);

        return new RemoveCartItemCommandHandler(
            _validator,
            _cartRepository.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private Cart CreateCart(
        string userId = "user-1",
        int hotelId = 10,
        int cartId = 20)
    {
        var cart =
            new Cart(
                userId,
                hotelId,
                _now.UtcDateTime.AddMinutes(-10));

        SetEntityId(
            cart,
            cartId);

        return cart;
    }

    private static void SetEntityId<T>(
        T entity,
        int id)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var field =
            typeof(T).GetField(
                "<Id>k__BackingField",
                BindingFlags.Instance |
                BindingFlags.NonPublic);

        if (field is null)
        {
            throw new InvalidOperationException(
                $"Could not find the Id backing field on " +
                $"{typeof(T).FullName}.");
        }

        field.SetValue(
            entity,
            id);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidationError_WhenRoomIdIsInvalid()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 0);

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

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                nameof(RemoveCartItemCommand.RoomId));

        _cartRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns((string?)null);

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
            "Authentication.Required",
            error.Code);

        _cartRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithoutSaving_WhenCartDoesNotExist()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _cartRepository
            .Setup(repository =>
                repository.GetByUserIdAsync(
                    "user-1",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

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

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _cartRepository.Verify(
            repository =>
                repository.Remove(
                    It.IsAny<Cart>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithoutSaving_WhenRoomIsNotInCart()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 2);

        var cart =
            CreateCart();

        cart.AddRoom(
            roomId: 1,
            addedAt:
                _now.UtcDateTime.AddMinutes(-5));

        var previousUpdatedAt =
            cart.UpdatedAt;

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _cartRepository
            .Setup(repository =>
                repository.GetByUserIdAsync(
                    "user-1",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

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

        Assert.Single(
            cart.Items);

        Assert.True(
            cart.ContainsRoom(1));

        Assert.Equal(
            previousUpdatedAt,
            cart.UpdatedAt);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _cartRepository.Verify(
            repository =>
                repository.Remove(
                    It.IsAny<Cart>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveRoomAndSave_WhenCartStillHasItems()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 2);

        var cart =
            CreateCart();

        cart.AddRoom(
            roomId: 1,
            addedAt:
                _now.UtcDateTime.AddMinutes(-6));

        cart.AddRoom(
            roomId: 2,
            addedAt:
                _now.UtcDateTime.AddMinutes(-5));

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _cartRepository
            .Setup(repository =>
                repository.GetByUserIdAsync(
                    "user-1",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

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

        Assert.Single(
            cart.Items);

        Assert.True(
            cart.ContainsRoom(1));

        Assert.False(
            cart.ContainsRoom(2));

        Assert.Equal(
            _now.UtcDateTime,
            cart.UpdatedAt);

        _cartRepository.Verify(
            repository =>
                repository.Remove(
                    It.IsAny<Cart>()),
            Times.Never);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveCart_WhenLastRoomIsRemoved()
    {
        // Arrange
        var command =
            new RemoveCartItemCommand(
                RoomId: 1);

        var cart =
            CreateCart();

        cart.AddRoom(
            roomId: 1,
            addedAt:
                _now.UtcDateTime.AddMinutes(-5));

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _cartRepository
            .Setup(repository =>
                repository.GetByUserIdAsync(
                    "user-1",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

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

        Assert.Empty(
            cart.Items);

        Assert.True(
            cart.IsEmpty());

        Assert.Equal(
            _now.UtcDateTime,
            cart.UpdatedAt);

        _cartRepository.Verify(
            repository =>
                repository.Remove(cart),
            Times.Once);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
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