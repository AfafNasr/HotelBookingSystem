using HotelBooking.Application.Common.Models;

namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public sealed record GetRecentlyVisitedHotelsResult(
    bool Succeeded,
    IReadOnlyCollection<RecentlyVisitedHotelItem> Hotels,
    IReadOnlyCollection<ApplicationError> Errors);