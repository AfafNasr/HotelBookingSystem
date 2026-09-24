using Azure;
using HotelBooking.Api.Common;
using HotelBooking.Api.Hotels.CreateHotel;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Reviews.CreateReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Reviews.CreateReview;

[ApiController]
[Route("api/bookings/{bookingId:int}/review")]
public sealed class CreateReviewEndpoint : ControllerBase
{
    private readonly CreateReviewCommandHandler _handler;

    public CreateReviewEndpoint(
        CreateReviewCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
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

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new CreateReviewResponse(
             result.ReviewId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}