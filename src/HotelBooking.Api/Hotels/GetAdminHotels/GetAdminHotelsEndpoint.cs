using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.GetAdminHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.GetAdminHotels;

[ApiController]
[Route("api/admin/hotels")]
public sealed class GetAdminHotelsEndpoint : ControllerBase
{
    private readonly GetAdminHotelsQueryHandler _handler;

    public GetAdminHotelsEndpoint(
        GetAdminHotelsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = HotelPermissions.View)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminHotelsQuery(search);

        var result = await _handler.HandleAsync(
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
}