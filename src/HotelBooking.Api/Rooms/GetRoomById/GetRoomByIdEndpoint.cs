using HotelBooking.Api.Common;
using HotelBooking.Application.Rooms.GetRoomById;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.GetRoomById;

[ApiController]
[Route("api/rooms")]
public sealed class GetRoomByIdEndpoint : ControllerBase
{
    private readonly GetRoomByIdQueryHandler _handler;

    public GetRoomByIdEndpoint(
        GetRoomByIdQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{roomId:int}")]
    public async Task<IActionResult> Get(
        int roomId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomByIdQuery(roomId);

        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Room);
    }
}