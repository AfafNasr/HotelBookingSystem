using HotelBooking.Api.Common;
using HotelBooking.Application.Amenities.CreateAmenity;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Amenities.CreateAmenity;

[ApiController]
[Route("api/admin/amenities")]
public sealed class CreateAmenityEndpoint : ControllerBase
{
    private readonly CreateAmenityCommandHandler _handler;

    public CreateAmenityEndpoint(
        CreateAmenityCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = AmenityPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateAmenityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAmenityCommand(
            request.Name);

        var result = await _handler.HandleAsync(
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
}