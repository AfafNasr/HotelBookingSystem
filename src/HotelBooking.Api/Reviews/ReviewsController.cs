using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Reviews.CreateReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Reviews;

[ApiController]
public sealed class ReviewsController : ControllerBase
{
    private readonly CreateReviewCommandHandler _createHandler;

    public ReviewsController(
        CreateReviewCommandHandler createHandler)
    {
        _createHandler = createHandler;
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
}