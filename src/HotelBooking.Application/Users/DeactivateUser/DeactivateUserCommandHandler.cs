using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Users.DeactivateUser;

public sealed class DeactivateUserCommandHandler
{
    private readonly IValidator<DeactivateUserCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public DeactivateUserCommandHandler(
        IValidator<DeactivateUserCommand> validator,
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
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
            return new DeactivateUserResult(
                false,
                [
                    new ApplicationError(
                        "User.CannotDeactivateSelf",
                        "An administrator cannot deactivate their own account.",
                        ErrorType.Conflict)
                ]);
        }

        return await _identityService.DeactivateUserAsync(
            command.UserId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }
}

public sealed record DeactivateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);