using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Bookings.GetMyBookings;

public sealed class GetMyBookingsQueryHandler
{
    private readonly IMyBookingsQuery _myBookingsQuery;
    private readonly ICurrentUserService _currentUserService;

    public GetMyBookingsQueryHandler(
        IMyBookingsQuery myBookingsQuery,
        ICurrentUserService currentUserService)
    {
        _myBookingsQuery = myBookingsQuery;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyBookingsResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new GetMyBookingsResult(
                false,
                [],
                [AuthenticationErrors.Required]);
        }

        var bookings = await _myBookingsQuery.GetAsync(
            userId,
            cancellationToken);

        return new GetMyBookingsResult(
            true,
            bookings,
            []);
    }
}

public sealed record GetMyBookingsResult(
    bool Succeeded,
    IReadOnlyCollection<MyBooking> Bookings,
    IReadOnlyCollection<ApplicationError> Errors);