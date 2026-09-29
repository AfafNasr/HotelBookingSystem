namespace HotelBooking.Application.Deals.UpdateDeal;

public sealed record UpdateDealCommand(
    int DealId,
    decimal DiscountPercentage,
    DateOnly StartDate,
    DateOnly EndDate);