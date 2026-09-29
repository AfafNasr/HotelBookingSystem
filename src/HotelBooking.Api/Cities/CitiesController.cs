using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.Application.Common.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HotelBooking.Api.Cities;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController : ControllerBase
{
    private readonly GetTrendingDestinationsQueryHandler _trendingHandler;
    private readonly IImageUrlProvider _imageUrlProvider;

    public CitiesController(
    GetTrendingDestinationsQueryHandler trendingHandler,
    IImageUrlProvider imageUrlProvider)
    {
        _trendingHandler = trendingHandler;
        _imageUrlProvider = imageUrlProvider;
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
                .Select(destination => new TrendingDestinationResponse(
        destination.CityId,
        destination.CityName,
      destination.ThumbnailStorageKey is null
    ? null
    : _imageUrlProvider.GetUrl(
        ImageContainer.CityImages,
        destination.ThumbnailStorageKey)))
    .ToList();

        return Ok(response);
    }

}