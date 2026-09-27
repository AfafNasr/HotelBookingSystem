using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler
{
    private readonly IValidator<DeactivateUserCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    public DeactivateUserCommandHandler(
        IValidator<DeactivateUserCommand> validator,
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _validator = validator;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<DeactivateUserResult> HandleAsync(
        DeactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new DeactivateUserResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var currentUserId =
            _currentUserService.UserId;

        if (string.Equals(
                currentUserId,
                command.UserId,
                StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                UserLog.SelfDeactivationAttempt(
                    _logger,
                    currentUserId);
            }

            return new DeactivateUserResult(
                false,
                [
                    new ApplicationError(
                        "User.CannotDeactivateSelf",
                        "An administrator cannot deactivate their own account.",
                        ErrorType.Conflict)
                ]);
        }

        var result =
            await _identityService.DeactivateUserAsync(
                command.UserId,
                _timeProvider.GetUtcNow().UtcDateTime,
                cancellationToken);

        if (result.Succeeded)
        {
            UserLog.UserDeactivated(
                _logger,
                command.UserId);
        }

        return result;
    }
}

public sealed record DeactivateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);