using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Deals;

public static class DealErrors
{
    public static readonly ApplicationError NotFound =
        new(
            "Deal.NotFound",
            "The deal was not found.",
            ErrorType.NotFound);

    public static readonly ApplicationError OverlappingDeal =
        new(
            "Deal.Overlapping",
            "The hotel already has a deal that overlaps with the selected date range.",
            ErrorType.Conflict);
}