using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Deals.CreateDeal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Deals.CreateDeal;

[ApiController]
[Route("api/hotels/{hotelId:int}/deals")]
public sealed class CreateDealEndpoint : ControllerBase
{
    private readonly CreateDealCommandHandler _handler;

    public CreateDealEndpoint(
        CreateDealCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [Authorize(Policy = DealPermissions.Create)]
    public async Task<IActionResult> Create(
        int hotelId,
        CreateDealRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDealCommand(
            hotelId,
            request.DiscountPercentage,
            request.StartDate,
            request.EndDate);

        var result = await _handler.HandleAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return ErrorResponseFactory.Create(
                this,
                result.Errors);
        }

        var response = new CreateDealResponse(
    result.DealId!.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}

public sealed record CreateDealRequest(
    decimal DiscountPercentage,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record CreateDealResponse(int DealId);