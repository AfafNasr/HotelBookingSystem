using HotelBooking.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Common;

public static class ErrorResponseFactory
{
    public static IActionResult Create(
        ControllerBase controller,
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
            return controller.Unauthorized(response);
        }

        if (errors.Any(error => error.Type == ErrorType.Conflict))
        {
            return controller.Conflict(response);
        }
        if (errors.Any(error => error.Type == ErrorType.NotFound))
        {
            return controller.NotFound(response);
        }
        if (errors.Any(error => error.Type == ErrorType.Authorization))
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, response);
        }

        return controller.BadRequest(response);
    }
}