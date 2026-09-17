using HotelBooking.Api.Authentication.Register;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerHandler;

    public AuthController(RegisterCommandHandler registerHandler)
    {
        _registerHandler = registerHandler;
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

        if (errors.Any(error => error.Type == ErrorType.Conflict))
        {
            return Conflict(response);
        }

        return BadRequest(response);
    }
}