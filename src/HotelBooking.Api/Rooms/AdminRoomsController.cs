using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.GetAdminRooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms;

[ApiController]
[Route("api/admin/rooms")]
public sealed class AdminRoomsController : ControllerBase
{
    private readonly GetAdminRoomsQueryHandler _getHandler;

    public AdminRoomsController(
        GetAdminRoomsQueryHandler getHandler)
    {
        _getHandler = getHandler;
    }

    [HttpGet]
    [Authorize(Policy = RoomPermissions.GetAdminRooms)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminRoomsQuery(search);

        var result = await _getHandler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Rooms);
    }
}