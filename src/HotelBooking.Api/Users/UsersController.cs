using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Users;


[ApiController]
[Route("api/users/me")]
public sealed class UsersController : ControllerBase

{
    private readonly GetRecentlyVisitedHotelsQueryHandler _handler;
    private readonly IImageUrlProvider _imageUrlProvider;
    public UsersController(
     GetRecentlyVisitedHotelsQueryHandler handler,
     IImageUrlProvider imageUrlProvider)
    {
        _handler = handler;
        _imageUrlProvider = imageUrlProvider;
    }

    [HttpGet("recently-visited-hotels")]
    [Authorize(Policy = UserPermissions.ViewRecentlyVisited)]
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
            .Select(hotel => new RecentlyVisitedHotelResponse(
                hotel.HotelId,
                hotel.Name,
                hotel.CityName,
                hotel.StarRating,
                hotel.StartingPricePerNight,
               hotel.ThumbnailStorageKey is null
    ? null
    : _imageUrlProvider.GetUrl(
        ImageContainer.HotelImages,
        hotel.ThumbnailStorageKey)))
            .ToArray();

        return Ok(response);
    }
}

public sealed record RecentlyVisitedHotelResponse(
    int HotelId,
    string Name,
    string CityName,
    int StarRating,
    decimal? StartingPricePerNight,
    string? ThumbnailUrl);



