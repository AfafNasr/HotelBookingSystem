namespace HotelBooking.Application.Deals.CreateDeal;

public sealed record CreateDealCommand(
    int HotelId,
    decimal DiscountPercentage,
    DateOnly StartDate,
    DateOnly EndDate);