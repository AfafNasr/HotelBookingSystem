using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.CreateRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.CreateRoom;

[ApiController]
[Route("api/hotels/{hotelId:int}/rooms")]
public sealed class CreateRoomEndpoint : ControllerBase
{
    private readonly CreateRoomCommandHandler _handler;

    public CreateRoomEndpoint(CreateRoomCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = RoomPermissions.Create)]
    public async Task<IActionResult> Create(
        int hotelId,
        CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoomCommand(
            hotelId,
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

        return Created(
            $"/api/hotels/{hotelId}/rooms/{result.RoomId}",
            new CreateRoomResponse(result.RoomId!.Value));
    }
}