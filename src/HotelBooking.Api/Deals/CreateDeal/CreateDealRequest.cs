namespace HotelBooking.Api.Deals.CreateDeal;

public sealed record CreateDealRequest(
    decimal DiscountPercentage,
    DateOnly StartDate,
    DateOnly EndDate);