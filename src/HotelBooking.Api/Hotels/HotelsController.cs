using HotelBooking.Api.Common;
using HotelBooking.Application.Bookings.GetHotelBookings;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Application.Hotels.SearchHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Hotels;


[ApiController]
[Route("api/hotels")]
public sealed class HotelsController : ControllerBase
{
    private readonly SearchHotelsQueryHandler _searchHandler;
    private readonly GetHotelBookingsQueryHandler _getHotelBookingsHandler;
    private readonly GetHotelDetailsQueryHandler _detailsHandler;

    public HotelsController(
        SearchHotelsQueryHandler searchHandler,
        GetHotelDetailsQueryHandler detailsHandler,
        GetHotelBookingsQueryHandler getHotelBookingsHandler)
    {
        _searchHandler = searchHandler;
        _detailsHandler = detailsHandler;
        _getHotelBookingsHandler = getHotelBookingsHandler;
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
            request.MinPrice,
            request.MaxPrice,
            request.StarRating,
            request.Category,
            request.AmenityIds,
            request.SortBy,
            request.Page,
            request.PageSize);

        var result = await _searchHandler.HandleAsync(
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


    [HttpGet("{hotelId:int}")]
    public async Task<IActionResult> GetDetails(
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

        var result = await _detailsHandler.HandleAsync(
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

    [HttpGet("{hotelId:int}/bookings")]
    [Authorize(Policy = BookingPermissions.ViewHotelBookings)]
    public async Task<IActionResult> GetHotelBookings(
    int hotelId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var query = new GetHotelBookingsQuery(
            hotelId,
            page,
            pageSize);

        var result = await _getHotelBookingsHandler.HandleAsync(
            query,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(new
        {
            result.Bookings,
            result.Page,
            result.PageSize,
            result.HasNextPage
        });
    }

}