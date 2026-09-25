using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.CreateRoom;
using HotelBooking.Application.Rooms.DeleteRoom;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Application.Rooms.GetHotelRooms;
using HotelBooking.Application.Rooms.GetRoomById;
using HotelBooking.Application.Rooms.UpdateRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Rooms;

[ApiController]
public sealed class RoomsController : ControllerBase
{
    private readonly CreateRoomCommandHandler _createHandler;
    private readonly UpdateRoomCommandHandler _updateHandler;
    private readonly DeleteRoomCommandHandler _deleteHandler;
    private readonly GetHotelRoomsQueryHandler _getHotelRoomsHandler;
    private readonly GetRoomByIdQueryHandler _getRoomByIdHandler;
    private readonly GetAvailableRoomsQueryHandler _getAvailableRoomsHandler;

    public RoomsController(
        CreateRoomCommandHandler createHandler,
        UpdateRoomCommandHandler updateHandler,
        DeleteRoomCommandHandler deleteHandler,
        GetHotelRoomsQueryHandler getHotelRoomsHandler,
        GetRoomByIdQueryHandler getRoomByIdHandler,
        GetAvailableRoomsQueryHandler getAvailableRoomsHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getHotelRoomsHandler = getHotelRoomsHandler;
        _getRoomByIdHandler = getRoomByIdHandler;
        _getAvailableRoomsHandler = getAvailableRoomsHandler;
    }

    [HttpPost("api/hotels/{hotelId:int}/rooms")]
    [Authorize(Policy = RoomPermissions.Create)]
    public async Task<IActionResult> Create(
        int hotelId,
        SaveRoomRequest request,
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

        var result = await _createHandler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateRoomResponse(result.RoomId!.Value));
    }

    [HttpDelete("api/rooms/{roomId:int}")]
    [Authorize(Policy = RoomPermissions.Delete)]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoomCommand(roomId);

        var result = await _deleteHandler.HandleAsync(
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

    [HttpGet("api/hotels/{hotelId:int}/rooms")]
    [Authorize(Policy = RoomPermissions.View)]
    public async Task<IActionResult> GetHotelRooms(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var query = new GetHotelRoomsQuery(hotelId);

        var result = await _getHotelRoomsHandler.HandleAsync(
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

    [HttpGet("api/rooms/{roomId:int}")]
    public async Task<IActionResult> GetById(
        int roomId,
        CancellationToken cancellationToken)
    {
        var query = new GetRoomByIdQuery(roomId);

        var result = await _getRoomByIdHandler.HandleAsync(
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

    [HttpGet("api/hotels/{hotelId:int}/rooms/available")]
    public async Task<IActionResult> GetAvailable(
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

        var result = await _getAvailableRoomsHandler.HandleAsync(
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

    [HttpPut("api/rooms/{roomId:int}")]
    [Authorize(Policy = RoomPermissions.Update)]
    public async Task<IActionResult> Update(
    int roomId,
    SaveRoomRequest request,
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

        var result = await _updateHandler.HandleAsync(
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
