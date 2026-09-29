using FluentValidation;

namespace HotelBooking.Application.Carts.RemoveCartItem;

public sealed class RemoveCartItemCommandValidator
    : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator()
    {
        RuleFor(command => command.RoomId)
            .GreaterThan(0);
    }
}