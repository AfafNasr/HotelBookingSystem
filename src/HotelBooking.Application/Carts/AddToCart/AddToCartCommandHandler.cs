using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Rooms;
using HotelBooking.Domain.Carts;

namespace HotelBooking.Application.Carts.AddToCart;

public sealed class AddToCartCommandHandler
{
    private readonly IValidator<AddToCartCommand> _validator;
    private readonly ICartRepository _cartRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public AddToCartCommandHandler(
        IValidator<AddToCartCommand> validator,
        ICartRepository cartRepository,
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _cartRepository = cartRepository;
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<AddToCartResult> HandleAsync(
        AddToCartCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new AddToCartResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new AddToCartResult(
                false,
                null,
                [AuthenticationErrors.Required]);
        }

        var room =
            await _roomRepository.GetByIdAsync(
                command.RoomId,
                cancellationToken);

        if (room is null)
        {
            return new AddToCartResult(
                false,
                null,
                [CartErrors.RoomNotFound]);
        }

        var cart =
            await _cartRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        if (cart is null)
        {
            cart =
                new Cart(
                    userId,
                    room.HotelId,
                    now);

            cart.AddRoom(
                room.Id,
                now);

            _cartRepository.Add(cart);

            await _cartRepository.SaveChangesAsync(
                cancellationToken);

            return new AddToCartResult(
                true,
                cart.Id,
                []);
        }

        if (cart.HotelId != room.HotelId)
        {
            return new AddToCartResult(
                false,
                cart.Id,
                [CartErrors.DifferentHotel]);
        }

        if (cart.ContainsRoom(room.Id))
        {
            return new AddToCartResult(
                true,
                cart.Id,
                []);
        }

        cart.AddRoom(
            room.Id,
            now);

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return new AddToCartResult(
            true,
            cart.Id,
            []);
    }
}

public sealed record AddToCartResult(
    bool Succeeded,
    int? CartId,
    IReadOnlyCollection<ApplicationError> Errors);