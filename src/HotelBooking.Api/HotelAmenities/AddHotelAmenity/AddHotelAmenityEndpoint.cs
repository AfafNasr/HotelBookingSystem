using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.HotelAmenities.AddHotelAmenity;

[ApiController]
[Route("api/owner/hotels/{hotelId:int}/amenities")]
public sealed class AddHotelAmenityEndpoint : ControllerBase
{
    private readonly AddHotelAmenityCommandHandler _handler;

    public AddHotelAmenityEndpoint(
        AddHotelAmenityCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = HotelAmenityPermissions.Manage)]
    public async Task<IActionResult> Add(
        int hotelId,
        AddHotelAmenityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddHotelAmenityCommand(
            hotelId,
            request.AmenityId);

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