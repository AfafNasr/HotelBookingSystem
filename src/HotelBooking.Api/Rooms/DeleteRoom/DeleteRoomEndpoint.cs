using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.DeleteRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.DeleteRoom;

[ApiController]
[Route("api/rooms")]
public sealed class DeleteRoomEndpoint : ControllerBase
{
    private readonly DeleteRoomCommandHandler _handler;

    public DeleteRoomEndpoint(
        DeleteRoomCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpDelete("{roomId:int}")]
    [Authorize(Policy = RoomPermissions.Delete)]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoomCommand(roomId);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }
}