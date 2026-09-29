using FluentValidation;
using HotelBooking.Application.Authentication;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Users.CreateUser;

public sealed class CreateUserCommandHandler
{
    private readonly IValidator<CreateUserCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IValidator<CreateUserCommand> validator,
        IIdentityService identityService,
         ILogger<CreateUserCommandHandler> logger)
    {
        _validator = validator;
        _identityService = identityService;
        _logger = logger;
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


        if (result.Succeeded &&
            result.UserId is not null)
        {
            UserLog.UserCreated(
                _logger,
                result.UserId);
        }

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