using FluentValidation;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Authentication.Login;

public sealed class LoginCommandHandler
{
    private readonly IValidator<LoginCommand> _validator;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IValidator<LoginCommand> validator,
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _validator = validator;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> HandleAsync(
        LoginCommand command )
        
    {
        var validationResult =
            await _validator.ValidateAsync(command);

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
            return new LoginResult(
                false,
                null,
                new[]
                {
                    new ApplicationError(
                        "InvalidCredentials",
                        "Invalid username or password.",
                        ErrorType.Authentication)
                });
        }

        var accessToken = _tokenService.CreateToken(user);

        return new LoginResult(
            true,
            accessToken,
            Array.Empty<ApplicationError>());
    }
}