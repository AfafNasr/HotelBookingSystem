using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Authentication.Register;

public sealed class RegisterCommandHandler
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterCommand> _validator;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IValidator<RegisterCommand> validator,
         ILogger<RegisterCommandHandler> logger)
    {
        _identityService = identityService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<RegisterResult> HandleAsync(
        RegisterCommand command , CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command , cancellationToken);

        if (!validationResult.IsValid)
        {
            return new RegisterResult(
                false,
                null,
                validationResult.ToApplicationErrors());
        }

        var result =
            await _identityService.CreateCustomerAsync(
                command.Username,
                command.Email,
                command.Password,
                cancellationToken);

        if (result.Succeeded &&
            result.UserId is not null)
        {
            AuthenticationLog.UserRegistered(
                _logger,
                result.UserId);
        }

        return result;
    }
}

public sealed record RegisterResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyCollection<ApplicationError> Errors);