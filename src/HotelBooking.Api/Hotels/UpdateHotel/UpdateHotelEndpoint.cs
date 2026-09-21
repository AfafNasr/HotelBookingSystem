using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.UpdateHotel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.UpdateHotel;

[ApiController]
[Route("api/admin/hotels")]
public sealed class UpdateHotelEndpoint : ControllerBase
{
    private readonly UpdateHotelCommandHandler _handler;

    public UpdateHotelEndpoint(
        UpdateHotelCommandHandler handler)
    {
        _handler = handler;
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