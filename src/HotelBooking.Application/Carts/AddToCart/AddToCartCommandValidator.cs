using FluentValidation;

namespace HotelBooking.Application.Carts.AddToCart;

public sealed class AddToCartCommandValidator
    : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(command => command.RoomId)
            .GreaterThan(0);
    }
}