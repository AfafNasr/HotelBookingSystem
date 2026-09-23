using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.GetRecentlyVisitedHotels;

[ApiController]
[Route("api/users/me/recently-visited-hotels")]

public sealed class GetRecentlyVisitedHotelsEndpoint : ControllerBase
{
    private readonly GetRecentlyVisitedHotelsQueryHandler _handler;

    public GetRecentlyVisitedHotelsEndpoint(
        GetRecentlyVisitedHotelsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [Authorize(Policy = HotelPermissions.ViewRecentlyVisited)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = result.Hotels
            .Select(hotel => new GetRecentlyVisitedHotelsResponse(
                hotel.HotelId,
                hotel.Name,
                hotel.CityName,
                hotel.StarRating,
                hotel.StartingPricePerNight,
                hotel.ThumbnailStorageKey))
            .ToArray();

        return Ok(response);
    }
}