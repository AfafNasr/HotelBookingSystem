using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Users.UpdateUser;

public sealed class UpdateUserCommandHandler
{
    private readonly IValidator<UpdateUserCommand> _validator;
    private readonly IIdentityService _identityService;

    public UpdateUserCommandHandler(
        IValidator<UpdateUserCommand> validator,
        IIdentityService identityService)
    {
        _validator = validator;
        _identityService = identityService;
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

        return await _identityService.UpdateUserAsync(
            command.UserId,
            command.UserName,
            command.Email,
            cancellationToken);
    }
}

public sealed record UpdateUserResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);