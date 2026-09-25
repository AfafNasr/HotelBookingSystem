using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Reviews.GetMyReviews;

public sealed class GetMyReviewsQueryHandler
{
    private readonly IGetMyReviewsQuery _query;
    private readonly ICurrentUserService _currentUserService;

    public GetMyReviewsQueryHandler(
        IGetMyReviewsQuery query,
        ICurrentUserService currentUserService)
    {
        _query = query;
        _currentUserService = currentUserService;
    }

    public async Task<GetMyReviewsResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new GetMyReviewsResult(
                false,
                [],
                [AuthenticationErrors.Required]);
        }

        var reviews = await _query.GetAsync(
            userId,
            cancellationToken);

        return new GetMyReviewsResult(
            true,
            reviews,
            []);
    }
}