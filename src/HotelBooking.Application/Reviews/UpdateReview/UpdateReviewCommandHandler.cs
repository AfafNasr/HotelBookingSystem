using FluentValidation;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Extensions;
using HotelBooking.Application.Common.Security;

namespace HotelBooking.Application.Reviews.UpdateReview;

public sealed class UpdateReviewCommandHandler
{
    private readonly IValidator<UpdateReviewCommand> _validator;
    private readonly IReviewRepository _reviewRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public UpdateReviewCommandHandler(
        IValidator<UpdateReviewCommand> validator,
        IReviewRepository reviewRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _validator = validator;
        _reviewRepository = reviewRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateReviewResult> HandleAsync(
        UpdateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            command,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return new UpdateReviewResult(
                false,
                validationResult.ToApplicationErrors());
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new UpdateReviewResult(
                false,
                [AuthenticationErrors.Required]);
        }

        var review = await _reviewRepository.GetByIdForUserAsync(
            command.ReviewId,
            userId,
            cancellationToken);

        if (review is null)
        {
            return new UpdateReviewResult(
                false,
                [ReviewErrors.NotFound]);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        review.Update(
            command.Rating,
            command.Comment,
            now);

        await _reviewRepository.SaveChangesAsync(
            cancellationToken);

        return new UpdateReviewResult(
            true,
            []);
    }
}

public sealed record UpdateReviewResult(
    bool Succeeded,
    IReadOnlyCollection<ApplicationError> Errors);