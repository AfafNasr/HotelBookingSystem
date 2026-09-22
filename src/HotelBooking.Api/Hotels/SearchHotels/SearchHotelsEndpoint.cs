using HotelBooking.Api.Common;
using HotelBooking.Application.Hotels.SearchHotels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.SearchHotels;

[ApiController]
[Route("api/hotels")]
public sealed class SearchHotelsEndpoint : ControllerBase
{
    private readonly SearchHotelsQueryHandler _handler;

    public SearchHotelsEndpoint(
        SearchHotelsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] SearchHotelsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new SearchHotelsQuery(
            request.Destination,
            request.CheckInDate,
            request.CheckOutDate,
            request.Adults,
            request.Children,
            request.Rooms,
            request.Page,
            request.PageSize);

        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new SearchHotelsResponse(
            result.Hotels
                .Select(hotel => new SearchHotelResponse(
                    hotel.HotelId,
                    hotel.Name,
                    hotel.CityName,
                    hotel.StarRating,
                    hotel.Description,
                    hotel.StartingPricePerNight,
                    hotel.ThumbnailStorageKey))
                .ToArray(),
            result.Page,
            result.PageSize,
            result.HasNextPage);

        return Ok(response);
    }
}