using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Deals.CreateDeal;

public sealed record CreateDealResult(
    bool Succeeded,
    int? DealId,
    IReadOnlyCollection<ApplicationError> Errors);