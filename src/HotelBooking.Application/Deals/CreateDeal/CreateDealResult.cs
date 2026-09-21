using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Deals.CreateDeal;

public sealed record CreateDealResult(
    bool Succeeded,
    int? DealId,
    IReadOnlyCollection<ApplicationError> Errors);