using HotelBooking.Application.Cities.GetTrendingDestinations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HotelBooking.Api.Cities;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController : ControllerBase
{
    private readonly GetTrendingDestinationsQueryHandler _trendingHandler;

    public CitiesController(
        GetTrendingDestinationsQueryHandler trendingHandler)
    {
        _trendingHandler = trendingHandler;

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

}