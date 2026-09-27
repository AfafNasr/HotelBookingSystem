using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;

namespace HotelBooking.Application.Users.CreateUser;

public sealed class CreateUserCommandHandler
{
    private readonly IValidator<CreateUserCommand> _validator;
    private readonly IIdentityService _identityService;

    public CreateUserCommandHandler(
        IValidator<CreateUserCommand> validator,
        IIdentityService identityService)
    {
        _validator = validator;
        _identityService = identityService;
    }

    public async Task<CreateUserResult> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult =
            await _validator.ValidateAsync(
                command,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            return new CreateUserResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var result =
            await _identityService.CreateCustomerAsync(
                command.UserName,
                command.Email,
                command.Password,
                cancellationToken);

        return new CreateUserResult(
            result.Succeeded,
            result.UserId,
            result.Errors);
    }
}

public sealed record CreateUserResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyCollection<ApplicationError> Errors);