using HotelBooking.Api.Common;
using HotelBooking.Application.Common.Security.Authorization.Permissions;
using HotelBooking.Application.Deals.CreateDeal;
using HotelBooking.Application.Deals.GetFeaturedDeals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Deals;

[ApiController]
public sealed class DealsController : ControllerBase
{
    private readonly CreateDealCommandHandler _createHandler;
    private readonly GetFeaturedDealsQueryHandler _featuredHandler;

    public DealsController(
        CreateDealCommandHandler createHandler,
        GetFeaturedDealsQueryHandler featuredHandler)
    {
        _createHandler = createHandler;
        _featuredHandler = featuredHandler;
    }

    [HttpPost("api/hotels/{hotelId:int}/deals")]
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
            new CreateDealResponse(result.DealId!.Value));
    }

    [HttpGet("api/hotels/featured-deals")]
    public async Task<IActionResult> GetFeatured(
        CancellationToken cancellationToken)
    {
        var deals = await _featuredHandler.HandleAsync(
            cancellationToken);

        var response = new GetFeaturedDealsResponse(
            deals
                .Select(deal => new FeaturedDealResponse(
                    deal.HotelId,
                    deal.HotelName,
                    deal.CityName,
                    deal.Address,
                    deal.StarRating,
                    deal.DiscountPercentage,
                    deal.OriginalPricePerNight,
                    deal.DiscountedPricePerNight,
                    deal.ThumbnailStorageKey))
                .ToArray());

        return Ok(response);
    }
}

public sealed record CreateDealRequest(
    decimal DiscountPercentage,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record CreateDealResponse(
    int DealId);

public sealed record GetFeaturedDealsResponse(
    IReadOnlyCollection<FeaturedDealResponse> Deals);

public sealed record FeaturedDealResponse(
    int HotelId,
    string HotelName,
    string CityName,
    string? Address,
    int StarRating,
    decimal DiscountPercentage,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight,
    string? ThumbnailStorageKey);
