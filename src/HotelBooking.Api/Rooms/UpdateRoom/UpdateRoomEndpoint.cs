using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.UpdateRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.UpdateRoom;

[ApiController]
[Route("api/rooms")]
public sealed class UpdateRoomEndpoint : ControllerBase
{
    private readonly UpdateRoomCommandHandler _handler;

    public UpdateRoomEndpoint(
        UpdateRoomCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPut("{roomId:int}")]
    [Authorize(Policy = RoomPermissions.Update)]
    public async Task<IActionResult> Update(
        int roomId,
        [FromBody] UpdateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoomCommand(
            roomId,
            request.RoomNumber,
            request.RoomType,
            request.Description,
            request.AdultsCapacity,
            request.ChildrenCapacity,
            request.PricePerNight);

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