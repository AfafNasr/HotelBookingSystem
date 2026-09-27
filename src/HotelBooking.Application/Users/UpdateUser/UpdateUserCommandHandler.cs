using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Users.UpdateUser;

public sealed class UpdateUserCommandHandler
{
    private readonly IValidator<UpdateUserCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IValidator<UpdateUserCommand> validator,
        IIdentityService identityService,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _validator = validator;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<UpdateUserResult> HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateUserResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var result =
            await _identityService.UpdateUserAsync(
                command.UserId,
                command.UserName,
                command.Email,
                cancellationToken);

        if (result.Succeeded)
        {
            UserLog.UserUpdated(
                _logger,
                command.UserId);
        }

        return result;
    }
}

public sealed record UpdateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);