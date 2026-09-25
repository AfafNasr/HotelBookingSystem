using HotelBooking.Api.Common;
using HotelBooking.Application.Amenities.CreateAmenity;
using HotelBooking.Application.Amenities.DeleteAmenity;
using HotelBooking.Application.Amenities.UpdateAmenity;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Amenities;

[ApiController]
[Route("api/admin/amenities")]
public sealed class AmenitiesController : ControllerBase
{
    private readonly CreateAmenityCommandHandler _createHandler;
    private readonly UpdateAmenityCommandHandler _updateHandler;
    private readonly DeleteAmenityCommandHandler _deleteHandler;

    public AmenitiesController(
    CreateAmenityCommandHandler createHandler,
    UpdateAmenityCommandHandler updateHandler,
    DeleteAmenityCommandHandler deleteHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpPost]
    [Authorize(Policy = AmenityPermissions.Create)]
    public async Task<IActionResult> Create(
       CreateAmenityRequest request,
       CancellationToken cancellationToken)
    {
        var command = new CreateAmenityCommand(
            request.Name);

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
            new CreateAmenityResponse(
                result.AmenityId!.Value));
    }


    [HttpPut("{amenityId:int}")]
    [Authorize(Policy = AmenityPermissions.Update)]
    public async Task<IActionResult> Update(
        int amenityId,
        UpdateAmenityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAmenityCommand(
            amenityId,
            request.Name);

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

    [HttpDelete("{amenityId:int}")]
    [Authorize(Policy = AmenityPermissions.Delete)]
    public async Task<IActionResult> Delete(
    int amenityId,
    CancellationToken cancellationToken)
    {
        var command = new DeleteAmenityCommand(
            amenityId);

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



public sealed record CreateAmenityRequest(
    string Name);

public sealed record CreateAmenityResponse(
    int AmenityId);

public sealed record UpdateAmenityRequest(
    string Name);

