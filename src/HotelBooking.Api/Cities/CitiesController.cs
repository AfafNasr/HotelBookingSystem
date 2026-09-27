using HotelBooking.Api.Common;
using HotelBooking.Application.Cities.DeleteCity;
using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.Application.Common.Security;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HotelBooking.Api.Cities;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController : ControllerBase
{
    private readonly GetTrendingDestinationsQueryHandler _trendingHandler;
    private readonly DeleteCityCommandHandler _deleteCityCommandHandler;

    public CitiesController(
        GetTrendingDestinationsQueryHandler trendingHandler,
        DeleteCityCommandHandler deleteCityCommandHandler)
    {
        _trendingHandler = trendingHandler;
        _deleteCityCommandHandler = deleteCityCommandHandler;
    }

    [HttpGet("trending")]
    [OutputCache(PolicyName = "TrendingDestinations")]
    public async Task<IActionResult> GetTrending(
    CancellationToken cancellationToken)
    {
        var result =
            await _trendingHandler.HandleAsync(
                cancellationToken);

        var response =
            result.Destinations
                .Select(destination =>
                    new TrendingDestinationResponse(
                        destination.CityId,
                        destination.CityName,
                        destination.ThumbnailStorageKey))
                .ToArray();

        return Ok(response);
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