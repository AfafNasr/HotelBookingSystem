using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.DeleteHotel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.DeleteHotel;

[ApiController]
[Route("api/admin/hotels")]
public sealed class DeleteHotelEndpoint : ControllerBase
{
    private readonly DeleteHotelCommandHandler _handler;

    public DeleteHotelEndpoint(
        DeleteHotelCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpDelete("{hotelId:int}")]
    [Authorize(Policy = HotelPermissions.Delete)]
    public async Task<IActionResult> Delete(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteHotelCommand(hotelId);

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