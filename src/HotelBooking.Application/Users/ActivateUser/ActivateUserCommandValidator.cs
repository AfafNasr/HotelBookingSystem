using FluentValidation;

namespace HotelBooking.Application.Users.ActivateUser;

public sealed class ActivateUserCommandValidator
    : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}