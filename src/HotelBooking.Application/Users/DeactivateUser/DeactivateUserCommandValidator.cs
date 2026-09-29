using FluentValidation;

namespace HotelBooking.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandValidator
    : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}