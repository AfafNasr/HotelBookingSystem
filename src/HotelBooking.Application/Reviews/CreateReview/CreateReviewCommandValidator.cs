using FluentValidation;

namespace HotelBooking.Application.Reviews.CreateReview;

public sealed class CreateReviewCommandValidator
    : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .GreaterThan(0);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}