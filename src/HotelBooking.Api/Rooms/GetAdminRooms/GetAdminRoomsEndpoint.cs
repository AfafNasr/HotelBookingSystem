using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.GetAdminRooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.GetAdminRooms;

[ApiController]
[Route("api/admin/rooms")]
public sealed class GetAdminRoomsEndpoint : ControllerBase
{
    private readonly GetAdminRoomsQueryHandler _handler;

    public GetAdminRoomsEndpoint(
        GetAdminRoomsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = RoomPermissions.GetAdminRooms)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminRoomsQuery(search);

        var result = await _handler.HandleAsync(
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