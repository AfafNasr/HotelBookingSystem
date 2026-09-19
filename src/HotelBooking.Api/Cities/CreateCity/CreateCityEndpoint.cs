using HotelBooking.Api.Common;
using HotelBooking.Application.Cities.CreateCity;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace HotelBooking.Api.Cities.CreateCity;

[ApiController]
[Route("api/admin/cities")]
public sealed class CreateCityEndpoint : ControllerBase
{
    private readonly CreateCityCommandHandler _handler;

    public CreateCityEndpoint(CreateCityCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = CityPermissions.Create)]
    public async Task<IActionResult> Create(
        CreateCityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCityCommand(
            request.Name,
            request.CountryCode,
            request.PostOffice);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(this, result.Errors);
        }

        var response = new CreateCityResponse(
            result.CityId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}