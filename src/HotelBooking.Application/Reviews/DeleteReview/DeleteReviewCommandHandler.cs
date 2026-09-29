using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Reviews.DeleteReview;

public sealed class DeleteReviewCommandHandler
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteReviewCommandHandler(
        IReviewRepository reviewRepository,
        ICurrentUserService currentUserService)
    {
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteReviewResult> HandleAsync(
        DeleteReviewCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ReviewId <= 0)
        {
            return new DeleteReviewResult(
                false,
                [
                    new ApplicationError(
                        "Review.InvalidId",
                        "The review ID must be greater than zero.",
                        ErrorType.Validation)
                ]);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new DeleteReviewResult(
                false,
                [AuthenticationErrors.Required]);
        }

        var review = await _reviewRepository.GetByIdForUserAsync(
            command.ReviewId,
            userId,
            cancellationToken);

        if (review is null)
        {
            return new DeleteReviewResult(
                false,
                [ReviewErrors.NotFound]);
        }

        _reviewRepository.Remove(review);

        await _reviewRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteReviewResult(
            true,
            []);
    }
}

public sealed record DeleteReviewResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);