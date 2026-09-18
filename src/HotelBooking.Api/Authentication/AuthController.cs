using HotelBooking.Api.Authentication.Login;
using HotelBooking.Api.Authentication.Register;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;

    public AuthController(RegisterCommandHandler registerHandler , LoginCommandHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password);

        var result = await _registerHandler.HandleAsync(command);

        if (!result.Succeeded)
        {
            return CreateErrorResponse(result.Errors);
        }

        var response = new RegisterResponse(
            result.UserId!,
            request.Username,
            request.Email);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
    LoginRequest request)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password);

        var result = await _loginHandler.HandleAsync(command);

        if (!result.Succeeded)
        {
            return CreateErrorResponse(result.Errors);
        }

        var response = new LoginResponse(
            result.AccessToken!.Token,
            result.AccessToken.ExpiresAt);

        return Ok(response);
    }

    private IActionResult CreateErrorResponse(
    IReadOnlyCollection<ApplicationError> errors)
    {
        var response = new
        {
            errors = errors.Select(error => new
            {
                error.Code,
                error.Description
            })
        };

        if (errors.Any(error => error.Type == ErrorType.Authentication))
        {
            return Unauthorized(response);
        }

        if (errors.Any(error => error.Type == ErrorType.Conflict))
        {
            return Conflict(response);
        }

        return BadRequest(response);
    }
}