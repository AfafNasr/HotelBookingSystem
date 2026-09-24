using HotelBooking.Api.Common;
using HotelBooking.Application.Hotels.GetHotelDetails;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels.GetHotelDetails;

[ApiController]
[Route("api/hotels/{hotelId:int}")]
public sealed class GetHotelDetailsEndpoint : ControllerBase
{
    private readonly GetHotelDetailsQueryHandler _handler;

    public GetHotelDetailsEndpoint(
        GetHotelDetailsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        int hotelId,
        [FromQuery] GetHotelDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetHotelDetailsQuery(
            hotelId,
            request.CheckInDate,
            request.CheckOutDate,
            request.Adults,
            request.Children,
            request.Rooms);

        var result = await _handler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var hotel = result.Hotel!;

        var response = new GetHotelDetailsResponse(
            hotel.Id,
            hotel.Name,
            hotel.CityName,
            hotel.StarRating,
            hotel.Category,
            hotel.Description,
            hotel.Address,
            hotel.Latitude,
            hotel.Longitude,
            hotel.AverageGuestRating,
            hotel.ReviewCount,
            hotel.RecentReviews
                .Select(review => new HotelReviewResponse(
                    review.Rating,
                    review.Comment,
                    review.CreatedAt))
                .ToArray(),

         hotel.AvailableRooms
        .Select(room => new AvailableRoomResponse(
            room.RoomType,
            room.AvailableCount))
        .ToArray());

        return Ok(response);
    }
}