using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Authentication.Login;

public sealed class LoginCommandHandler
{
    private readonly IValidator<LoginCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;


    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        IIdentityService identityService,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _validator = validator;
        _identityService = identityService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResult> HandleAsync(
        LoginCommand command, CancellationToken cancellationToken)
        
    {
        var validationResult =
            await _validator.ValidateAsync(command , cancellationToken);

        if (!validationResult.IsValid)
        {
            return new LoginResult(
                false,
                null,
               validationResult.ToApplicationErrors());
        }

        var user = await _identityService.AuthenticateAsync(
            command.Username,
            command.Password);

        if (user is null)
        {
            AuthenticationLog.LoginFailed(
                _logger,
                command.Username);

            return new LoginResult(
                false,
                null,
                [AuthenticationErrors.InvalidCredentials]);
        }

        var accessToken = _tokenService.CreateToken(user);

        AuthenticationLog.LoginSucceeded(
           _logger,
           command.Username);

        return new LoginResult(
            true,
            accessToken,
            Array.Empty<ApplicationError>());
    }
}

public sealed record LoginResult(
    bool Succeeded,
    AccessToken? AccessToken,
    IReadOnlyCollection<ApplicationError> Errors);