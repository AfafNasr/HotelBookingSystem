using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Reviews.CreateReview;
using HotelBooking.Application.Reviews.DeleteReview;
using HotelBooking.Application.Reviews.GetHotelReviews;
using HotelBooking.Application.Reviews.GetMyReviews;
using HotelBooking.Application.Reviews.UpdateReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Reviews;

[ApiController]
public sealed class ReviewsController : ControllerBase
{
    private readonly CreateReviewCommandHandler _createHandler;
    private readonly GetMyReviewsQueryHandler _getMyReviewsHandler;
    private readonly UpdateReviewCommandHandler _updateHandler;
    private readonly DeleteReviewCommandHandler _deleteHandler;
    private readonly GetHotelReviewsQueryHandler _getHotelReviewsHandler;

    public ReviewsController(
     CreateReviewCommandHandler createHandler,
     GetMyReviewsQueryHandler getMyReviewsHandler,
     GetHotelReviewsQueryHandler getHotelReviewsHandler,
     UpdateReviewCommandHandler updateHandler,
     DeleteReviewCommandHandler deleteHandler)
    {
        _createHandler = createHandler;
        _getMyReviewsHandler = getMyReviewsHandler;
        _getHotelReviewsHandler = getHotelReviewsHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpPost("api/bookings/{bookingId:int}/review")]
    [Authorize(Policy = ReviewPermissions.Create)]
    public async Task<IActionResult> Create(
        int bookingId,
        CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(
            bookingId,
            request.Rating,
            request.Comment);

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
            new CreateReviewResponse(
                result.ReviewId!.Value));
    }

    [HttpGet("api/reviews/me")]
    [Authorize(Policy = ReviewPermissions.View)]
    public async Task<IActionResult> GetMyReviews(
    CancellationToken cancellationToken)
    {
        var result = await _getMyReviewsHandler.HandleAsync(
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        return Ok(result.Reviews);
    }

    [HttpPut("api/reviews/{reviewId:int}")]
    [Authorize(Policy = ReviewPermissions.Update)]
    public async Task<IActionResult> Update(
    int reviewId,
    UpdateReviewRequest request,
    CancellationToken cancellationToken)
    {
        var command = new UpdateReviewCommand(
            reviewId,
            request.Rating,
            request.Comment);

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

    [HttpDelete("api/reviews/{reviewId:int}")]
    [Authorize(Policy = ReviewPermissions.Delete)]
    public async Task<IActionResult> Delete(
    int reviewId,
    CancellationToken cancellationToken)
    {
        var command = new DeleteReviewCommand(reviewId);

        var result = await _deleteHandler.HandleAsync(
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

    [HttpGet("api/hotels/{hotelId:int}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHotelReviews(
    int hotelId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var query = new GetHotelReviewsQuery(
            hotelId,
            page,
            pageSize);

        var result = await _getHotelReviewsHandler.HandleAsync(
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
            result.Reviews,
            result.Page,
            result.PageSize,
            result.HasNextPage
        });
    }
}