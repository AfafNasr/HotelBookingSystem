using HotelBooking.Api.Common;
using HotelBooking.Application.Cities.CreateCity;
using HotelBooking.Application.Cities.DeleteCity;
using HotelBooking.Application.Cities.GetAdminCities;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Cities;

[ApiController]
[Route("api/admin/cities")]
public sealed class AdminCitiesController : ControllerBase
{
    private readonly CreateCityCommandHandler _createHandler;
    private readonly GetAdminCitiesQueryHandler _getHandler;
    private readonly UpdateCityCommandHandler _updateHandler;
    private readonly DeleteCityCommandHandler _deleteCityCommandHandler;

    public AdminCitiesController(
        CreateCityCommandHandler createHandler,
        GetAdminCitiesQueryHandler getHandler,
        UpdateCityCommandHandler updateHandler,
        DeleteCityCommandHandler deleteCityCommandHandler)
    {
        _createHandler = createHandler;
        _getHandler = getHandler;
        _updateHandler = updateHandler;
        _deleteCityCommandHandler = deleteCityCommandHandler;
    }

    [HttpGet]
    [Authorize(Policy = CityPermissions.View)]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = new GetAdminCitiesQuery(search);

        var result = await _getHandler.HandleAsync(
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
            new CreateCityResponse(result.CityId!.Value));
    }

    [HttpPut("{cityId:int}")]
    [Authorize(Policy = CityPermissions.Update)]
    public async Task<IActionResult> Update(
        int cityId,
        UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCityCommand(
            cityId,
            request.Name,
            request.CountryCode,
            request.PostOffice);

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

    [HttpDelete("{cityId:int}")]
    [Authorize(Policy = CityPermissions.Delete)]
    public async Task<IActionResult> Delete(
 int cityId,
 CancellationToken cancellationToken)
    {
        var result = await _deleteCityCommandHandler.HandleAsync(
            new DeleteCityCommand(cityId),
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