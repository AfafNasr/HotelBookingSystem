using HotelBooking.Application.Deals.GetFeaturedDeals;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Api.Deals.GetFeaturedDeals;

[ApiController]
[Route("api/hotels")]
public sealed class GetFeaturedDealsEndpoint : ControllerBase
{
    private readonly GetFeaturedDealsQueryHandler _handler;

    public GetFeaturedDealsEndpoint(
        GetFeaturedDealsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("featured-deals")]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var deals = await _handler.HandleAsync(cancellationToken);

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
