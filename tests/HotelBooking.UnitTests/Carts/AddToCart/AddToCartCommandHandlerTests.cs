using System.Reflection;
using HotelBooking.Application.Carts;
using HotelBooking.Application.Carts.AddToCart;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Rooms;
using HotelBooking.Domain.Carts;
using HotelBooking.Domain.Rooms;
using Moq;

namespace HotelBooking.UnitTests.Carts.AddToCart;

public sealed class AddToCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly AddToCartCommandValidator _validator = new();

    private readonly DateTimeOffset _now =
        new(
            2026,
            9,
            28,
            10,
            0,
            0,
            TimeSpan.Zero);

    private AddToCartCommandHandler CreateHandler()
    {
        var timeProvider =
            new TestTimeProvider(_now);

        return new AddToCartCommandHandler(
            _validator,
            _cartRepository.Object,
            _roomRepository.Object,
            _currentUserService.Object,
            timeProvider);
    }

    private static Room CreateRoom(
        int id = 1,
        int hotelId = 10)
    {
        var room =
            new Room(
                hotelId,
                roomNumber: $"ROOM-{id}",
                RoomType.Standard,
                description: "Test room",
                adultsCapacity: 2,
                childrenCapacity: 1,
                pricePerNight: 100m,
                createdAt: DateTime.UtcNow);

        SetEntityId(
            room,
            id);

        return room;
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
            new AddToCartCommand(
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
        Assert.Null(result.CartId);
        Assert.NotEmpty(result.Errors);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code ==
                nameof(AddToCartCommand.RoomId));

        _roomRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _cartRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAuthenticationError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var command =
            new AddToCartCommand(
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
        Assert.Null(result.CartId);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Authentication.Required",
            error.Code);

        _roomRepository.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _cartRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRoomNotFound_WhenRoomDoesNotExist()
    {
        // Arrange
        var command =
            new AddToCartCommand(
                RoomId: 1);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var handler =
            CreateHandler();

        // Act
        var result =
            await handler.HandleAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.CartId);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Cart.RoomNotFound",
            error.Code);

        _cartRepository.Verify(
            repository =>
                repository.GetByUserIdAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateCartAndAddRoom_WhenUserHasNoCart()
    {
        // Arrange
        var command =
            new AddToCartCommand(
                RoomId: 5);

        var room =
            CreateRoom(
                id: command.RoomId,
                hotelId: 10);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _cartRepository
            .Setup(repository =>
                repository.GetByUserIdAsync(
                    "user-1",
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        Cart? createdCart = null;

        _cartRepository
            .Setup(repository =>
                repository.Add(
                    It.IsAny<Cart>()))
            .Callback<Cart>(
                cart =>
                {
                    createdCart = cart;

                    SetEntityId(
                        cart,
                        100);
                });

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

        Assert.NotNull(createdCart);

        Assert.Equal(
            "user-1",
            createdCart.UserId);

        Assert.Equal(
            room.HotelId,
            createdCart.HotelId);

        Assert.Equal(
            _now.UtcDateTime,
            createdCart.CreatedAt);

        Assert.Equal(
            _now.UtcDateTime,
            createdCart.UpdatedAt);

        var item =
            Assert.Single(
                createdCart.Items);

        Assert.Equal(
            room.Id,
            item.RoomId);

        Assert.Equal(
            _now.UtcDateTime,
            item.AddedAt);

        Assert.Equal(
            100,
            result.CartId);

        _cartRepository.Verify(
            repository =>
                repository.Add(
                    createdCart),
            Times.Once);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldAddRoom_WhenCartBelongsToSameHotel()
    {
        // Arrange
        var command =
            new AddToCartCommand(
                RoomId: 2);

        var room =
            CreateRoom(
                id: command.RoomId,
                hotelId: 10);

        var cart =
            CreateCart(
                userId: "user-1",
                hotelId: 10,
                cartId: 20);

        cart.AddRoom(
            roomId: 1,
            addedAt:
                _now.UtcDateTime.AddMinutes(-5));

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

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

        Assert.Equal(
            cart.Id,
            result.CartId);

        Assert.Equal(
            2,
            cart.Items.Count);

        Assert.True(
            cart.ContainsRoom(1));

        Assert.True(
            cart.ContainsRoom(2));

        Assert.Equal(
            _now.UtcDateTime,
            cart.UpdatedAt);

        _cartRepository.Verify(
            repository =>
                repository.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _cartRepository.Verify(
            repository =>
                repository.Add(
                    It.IsAny<Cart>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccessWithoutSaving_WhenRoomAlreadyExistsInCart()
    {
        // Arrange
        var command =
            new AddToCartCommand(
                RoomId: 1);

        var room =
            CreateRoom(
                id: command.RoomId,
                hotelId: 10);

        var cart =
            CreateCart(
                userId: "user-1",
                hotelId: 10,
                cartId: 20);

        var originalAddedAt =
            _now.UtcDateTime.AddMinutes(-5);

        cart.AddRoom(
            room.Id,
            originalAddedAt);

        var previousUpdatedAt =
            cart.UpdatedAt;

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

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

        Assert.Equal(
            cart.Id,
            result.CartId);

        var item =
            Assert.Single(
                cart.Items);

        Assert.Equal(
            room.Id,
            item.RoomId);

        Assert.Equal(
            originalAddedAt,
            item.AddedAt);

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
                repository.Add(
                    It.IsAny<Cart>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenRoomBelongsToDifferentHotel()
    {
        // Arrange
        var command =
            new AddToCartCommand(
                RoomId: 2);

        var room =
            CreateRoom(
                id: command.RoomId,
                hotelId: 99);

        var cart =
            CreateCart(
                userId: "user-1",
                hotelId: 10,
                cartId: 20);

        cart.AddRoom(
            roomId: 1,
            addedAt:
                _now.UtcDateTime.AddMinutes(-5));

        var previousUpdatedAt =
            cart.UpdatedAt;

        _currentUserService
            .Setup(service => service.UserId)
            .Returns("user-1");

        _roomRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    command.RoomId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

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
        Assert.False(result.Succeeded);

        Assert.Equal(
            cart.Id,
            result.CartId);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            "Cart.DifferentHotel",
            error.Code);

        Assert.Single(
            cart.Items);

        Assert.False(
            cart.ContainsRoom(
                command.RoomId));

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
                repository.Add(
                    It.IsAny<Cart>()),
            Times.Never);
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