using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Rooms.CreateRoom;
using HotelBooking.Application.Rooms.DeleteRoom;
using HotelBooking.Application.Rooms.GetAdminRooms;
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
    private readonly GetAdminRoomsQueryHandler _getAdminRoomsHandler;
    private readonly GetHotelRoomsQueryHandler _getHotelRoomsHandler;
    private readonly GetRoomByIdQueryHandler _getRoomByIdHandler;

    public RoomsController(
       CreateRoomCommandHandler createHandler,
       UpdateRoomCommandHandler updateHandler,
       DeleteRoomCommandHandler deleteHandler,
       GetAdminRoomsQueryHandler getAdminRoomsHandler,
       GetHotelRoomsQueryHandler getHotelRoomsHandler,
       GetRoomByIdQueryHandler getRoomByIdHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getAdminRoomsHandler = getAdminRoomsHandler;
        _getHotelRoomsHandler = getHotelRoomsHandler;
        _getRoomByIdHandler = getRoomByIdHandler;
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

        return Created(
            $"/api/hotels/{hotelId}/rooms/{result.RoomId}",
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

    [HttpGet("api/admin/rooms")]
    [Authorize(Policy = RoomPermissions.GetAdminRooms)]
    public async Task<IActionResult> GetAdminRooms(
       [FromQuery] string? search,
       CancellationToken cancellationToken)
    {
        var query = new GetAdminRoomsQuery(search);

        var result = await _getAdminRoomsHandler.HandleAsync(
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
    public async Task<IActionResult> GetRoomById(
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

}

