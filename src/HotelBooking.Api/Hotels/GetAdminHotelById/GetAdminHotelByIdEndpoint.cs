using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.GetAdminHotelById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.GetAdminHotelById;

[ApiController]
[Route("api/admin/hotels")]
public sealed class GetAdminHotelByIdEndpoint : ControllerBase
{
    private readonly GetAdminHotelByIdQueryHandler _handler;

    public GetAdminHotelByIdEndpoint(
        GetAdminHotelByIdQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{hotelId:int}")]
    [Authorize(Policy = HotelPermissions.GetAdminHotelById)]
    public async Task<IActionResult> Get(
        int hotelId,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(
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
}