using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Carts.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler
{
    private readonly IValidator<RemoveCartItemCommand> _validator;
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public RemoveCartItemCommandHandler(
        IValidator<RemoveCartItemCommand> validator,
        ICartRepository cartRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _cartRepository = cartRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<RemoveCartItemResult> HandleAsync(
        RemoveCartItemCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new RemoveCartItemResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var userId =
            _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new RemoveCartItemResult(
                false,
                [AuthenticationErrors.Required]);
        }

        var cart =
            await _cartRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

        if (cart is null)
        {
            return new RemoveCartItemResult(
                true,
                []);
        }

        var now =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        var removed =
            cart.RemoveRoom(
                command.RoomId,
                now);

        if (!removed)
        {
            return new RemoveCartItemResult(
                true,
                []);
        }

        if (cart.IsEmpty())
        {
            _cartRepository.Remove(cart);
        }

        await _cartRepository.SaveChangesAsync(
            cancellationToken);

        return new RemoveCartItemResult(
            true,
            []);
    }
}


public sealed record RemoveCartItemResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);