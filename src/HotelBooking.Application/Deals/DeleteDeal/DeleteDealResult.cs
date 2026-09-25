using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Deals.DeleteDeal;

public sealed record DeleteDealResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);