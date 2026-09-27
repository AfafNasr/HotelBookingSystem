using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Users.DeactivateUser;

namespace HotelBooking.Application.Users.ActivateUser;

public sealed class ActivateUserCommandHandler
{
    private readonly IValidator<ActivateUserCommand> _validator;
    private readonly IIdentityService _identityService;

    public ActivateUserCommandHandler(
        IValidator<ActivateUserCommand> validator,
        IIdentityService identityService)
    {
        _validator = validator;
        _identityService = identityService;
    }

    public async Task<ActivateUserResult> HandleAsync(
        ActivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new ActivateUserResult(
                false,
                validationResult.ToApplicationErrors());
        }

        return await _identityService.ActivateUserAsync(
            command.UserId,
            cancellationToken);
    }
}

public sealed record ActivateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);