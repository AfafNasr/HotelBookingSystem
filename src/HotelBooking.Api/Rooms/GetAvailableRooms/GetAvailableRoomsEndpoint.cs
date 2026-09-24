using HotelBooking.Api.Common;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms.GetAvailableRooms;

[ApiController]
[Route("api/hotels/{hotelId:int}/rooms/available")]
public sealed class GetAvailableRoomsEndpoint : ControllerBase
{
    private readonly GetAvailableRoomsQueryHandler _handler;

    public GetAvailableRoomsEndpoint(
        GetAvailableRoomsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        int hotelId,
        [FromQuery] GetAvailableRoomsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableRoomsQuery(
            hotelId,
            request.RoomType,
            request.CheckInDate,
            request.CheckOutDate,
            request.Adults,
            request.Children);

        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = result.Rooms
            .Select(room => new GetAvailableRoomsResponse(
                room.Id,
                room.RoomType,
                room.Description,
                room.AdultsCapacity,
                room.ChildrenCapacity,
                room.PricePerNight,
                room.Images
                    .Select(image => new AvailableRoomImageResponse(
                        image.StorageKey,
                        image.DisplayOrder,
                        image.IsPrimary))
                    .ToArray()))
            .ToArray();

        return Ok(response);
    }
}