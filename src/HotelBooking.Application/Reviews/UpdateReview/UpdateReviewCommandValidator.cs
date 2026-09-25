using FluentValidation;

namespace HotelBooking.Application.Reviews.UpdateReview;

public sealed class UpdateReviewCommandValidator
    : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId)
            .GreaterThan(0);

        RuleFor(command => command.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(command => command.Comment)
            .MaximumLength(2000);
    }
}