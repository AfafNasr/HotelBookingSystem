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
    private readonly PromoteToHotelOwnerCommandHandler _promoteHandler;

    public AdminUsersController(
        PromoteToHotelOwnerCommandHandler promoteHandler )
    {
        _promoteHandler = promoteHandler;
    }

    [HttpPost("{userId}/hotel-owner-role")]
    [Authorize(Policy = UserPermissions.PromoteToHotelOwner)]
    public async Task<IActionResult> PromoteToHotelOwner(
        string userId , CancellationToken cancellationToken)
    {
        var command = new PromoteToHotelOwnerCommand(userId);

        var result =
            await _promoteHandler.HandleAsync(command, cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }
}