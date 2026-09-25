using HotelBooking.Api.Common;
using HotelBooking.Application.Cities.GetAdminCities;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Cities.GetAdminCities;

[ApiController]
[Route("api/admin/cities")]
public sealed class GetAdminCitiesEndpoint : ControllerBase
{
    private readonly GetAdminCitiesQueryHandler _handler;

    public GetAdminCitiesEndpoint(
        GetAdminCitiesQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = CityPermissions.View)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminCitiesQuery(search);

        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Cities);
    }
}