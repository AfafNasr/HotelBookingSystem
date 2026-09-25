using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Application.Hotels.DeleteHotel;
using HotelBooking.Application.Hotels.GetAdminHotelById;
using HotelBooking.Application.Hotels.GetAdminHotels;
using HotelBooking.Application.Hotels.UpdateHotel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels;

[ApiController]
[Route("api/admin/hotels")]
public sealed class AdminHotelsController : ControllerBase

{
    private readonly CreateHotelCommandHandler _createHandler;
    private readonly GetAdminHotelsQueryHandler _getAllHandler;
    private readonly GetAdminHotelByIdQueryHandler _getByIdHandler;
    private readonly UpdateHotelCommandHandler _updateHandler;
    private readonly DeleteHotelCommandHandler _deleteHandler;

    public AdminHotelsController(
       CreateHotelCommandHandler createHandler,
       GetAdminHotelsQueryHandler getAllHandler,
       GetAdminHotelByIdQueryHandler getByIdHandler,
       UpdateHotelCommandHandler updateHandler,
       DeleteHotelCommandHandler deleteHandler)
    {
        _createHandler = createHandler;
        _getAllHandler = getAllHandler;
        _getByIdHandler = getByIdHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpGet]
    [Authorize(Policy = HotelPermissions.GetAdminHotels)]
    public async Task<IActionResult> GetAll(
       [FromQuery] string? search,
       CancellationToken cancellationToken)
    {
        var query = new GetAdminHotelsQuery(search);

        var result = await _getAllHandler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Hotels);
    }

    [HttpGet("{hotelId:int}")]
    [Authorize(Policy = HotelPermissions.GetAdminHotelById)]
    public async Task<IActionResult> GetById(
       int hotelId,
       CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(
            hotelId,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Hotel);
    }

    [HttpPost]
    [Authorize(Policy = HotelPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateHotelRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateHotelCommand(
            request.Name,
            request.CityId,
            request.OwnerId,
            request.StarRating,
            request.Category);

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
            new CreateHotelResponse(result.HotelId!.Value));
    }

    [HttpPut("{hotelId:int}")]
    [Authorize(Policy = HotelPermissions.Update)]
    public async Task<IActionResult> Update(
    int hotelId,
    UpdateHotelRequest request,
    CancellationToken cancellationToken)
    {
        var command = new UpdateHotelCommand(
            hotelId,
            request.Name,
            request.CityId,
            request.OwnerId,
            request.StarRating,
            request.Category,
            request.Description,
            request.Address,
            request.Latitude,
            request.Longitude);

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

    [HttpDelete("{hotelId:int}")]
    [Authorize(Policy = HotelPermissions.Delete)]
    public async Task<IActionResult> Delete(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteHotelCommand(hotelId); 

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

}