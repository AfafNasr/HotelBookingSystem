using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.GetHotelRooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.GetHotelRooms;

[ApiController]
[Route("api/hotels/{hotelId:int}/rooms")]

public sealed class GetHotelRoomsEndpoint : ControllerBase
{
    private readonly GetHotelRoomsQueryHandler _handler;

    public GetHotelRoomsEndpoint(
        GetHotelRoomsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = RoomPermissions.View)]
    public async Task<IActionResult> Get(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var query = new GetHotelRoomsQuery(hotelId);

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