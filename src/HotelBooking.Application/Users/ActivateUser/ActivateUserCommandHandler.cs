using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Users.DeactivateUser;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Users.ActivateUser;

public sealed class ActivateUserCommandHandler
{
    private readonly IValidator<ActivateUserCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ILogger<ActivateUserCommandHandler> _logger;


    public ActivateUserCommandHandler(
        IValidator<ActivateUserCommand> validator,
        IIdentityService identityService,
        ILogger<ActivateUserCommandHandler> logger)
    {
        _validator = validator;
        _identityService = identityService;
        _logger = logger;
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

        var result =
           await _identityService.ActivateUserAsync(
               command.UserId,
               cancellationToken);

        if (result.Succeeded)
        {
            UserLog.UserActivated(
                _logger,
                command.UserId);
        }

        return result;
    }
}

public sealed record ActivateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);