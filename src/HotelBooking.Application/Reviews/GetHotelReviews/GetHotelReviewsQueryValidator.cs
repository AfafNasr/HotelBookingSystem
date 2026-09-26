using FluentValidation;

namespace HotelBooking.Application.Reviews.GetHotelReviews;

public sealed class GetHotelReviewsQueryValidator
    : AbstractValidator<GetHotelReviewsQuery>
{
    public GetHotelReviewsQueryValidator()
    {
        RuleFor(query => query.HotelId)
            .GreaterThan(0);

        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 50);
    }
}