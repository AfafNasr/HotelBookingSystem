using HotelBooking.Application.Cities.GetTrendingDestinations;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Cities.GetTrendingDestinations;

[ApiController]
[Route("api/cities/trending")]
public sealed class GetTrendingDestinationsEndpoint : ControllerBase
{
    private readonly GetTrendingDestinationsQueryHandler _handler;

    public GetTrendingDestinationsEndpoint(
        GetTrendingDestinationsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(cancellationToken);

        var response = result.Destinations
            .Select(destination => new GetTrendingDestinationResponse(
                destination.CityId,
                destination.CityName,
                destination.ThumbnailStorageKey))
            .ToArray();

        return Ok(response);
    }
}