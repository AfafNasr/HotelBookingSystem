using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;

public sealed class GetRecentlyVisitedHotelsQueryHandler
{
    private const int RecentlyVisitedLimit = 5;

    private readonly ICurrentUserService _currentUserService;
    private readonly IRecentlyVisitedHotelsQuery _recentlyVisitedHotelsQuery;

    public GetRecentlyVisitedHotelsQueryHandler(
        ICurrentUserService currentUserService,
        IRecentlyVisitedHotelsQuery recentlyVisitedHotelsQuery)
    {
        _currentUserService = currentUserService;
        _recentlyVisitedHotelsQuery = recentlyVisitedHotelsQuery;
    }

    public async Task<GetRecentlyVisitedHotelsResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null)
        {
            return new GetRecentlyVisitedHotelsResult(
                false,
                [],
               [AuthenticationErrors.Required]);
        }

        var hotels = await _recentlyVisitedHotelsQuery.GetAsync(
            userId,
            RecentlyVisitedLimit,
            cancellationToken);

        return new GetRecentlyVisitedHotelsResult(
            true,
            hotels,
            []);
    }
}