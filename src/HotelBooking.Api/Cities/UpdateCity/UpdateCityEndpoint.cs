using HotelBooking.Api.Common;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Cities.UpdateCity;

[ApiController]
[Route("api/admin/cities")]
public sealed class UpdateCityEndpoint : ControllerBase
{
    private readonly UpdateCityCommandHandler _handler;

    public UpdateCityEndpoint(UpdateCityCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = CityPermissions.Update)]
    public async Task<IActionResult> Update(
        int id,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCityCommand(
            id,
            request.Name,
            request.CountryCode,
            request.PostOffice);

        var result =
            await _handler.HandleAsync(command, cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return NoContent();
    }
}