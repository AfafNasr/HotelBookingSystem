using HotelBooking.Application.Common.Errors;

namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public sealed record GetRecentlyVisitedHotelsResult(
    bool Succeeded,
    IReadOnlyCollection<RecentlyVisitedHotelItem> Hotels,
    IReadOnlyCollection<ApplicationError> Errors);