using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Users;

[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly PromoteToHotelOwnerHandler _promoteToHotelOwnerHandler;

    public AdminUsersController(
        PromoteToHotelOwnerHandler promoteToHotelOwnerHandler)
    {
        _promoteToHotelOwnerHandler = promoteToHotelOwnerHandler;
    }

    [HttpPost("{userId}/hotel-owner-role")]
    [Authorize(Policy = UserPermissions.PromoteToHotelOwner)]
    public async Task<IActionResult> PromoteToHotelOwner(
        string userId)
    {
        var command = new PromoteToHotelOwnerCommandHandler(userId);

        var result =
            await _promoteToHotelOwnerHandler.HandleAsync(command);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }
}