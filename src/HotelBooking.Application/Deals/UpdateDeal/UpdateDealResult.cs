using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Deals.UpdateDeal;

public sealed record UpdateDealResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);