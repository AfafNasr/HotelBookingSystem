using FluentValidation;

namespace HotelBooking.Application.Hotels.SearchHotels;

public sealed class SearchHotelsQueryValidator
    : AbstractValidator<SearchHotelsQuery>
{
    public SearchHotelsQueryValidator()
    {
        RuleFor(query => query.Destination)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(query => query.CheckInDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Check-in date cannot be in the past.");

        RuleFor(query => query.CheckOutDate)
            .GreaterThan(query => query.CheckInDate)
            .WithMessage("Check-out date must be after the check-in date.");

        RuleFor(query => query.Adults)
            .GreaterThan(0);

        RuleFor(query => query.Children)
            .GreaterThanOrEqualTo(0);

        RuleFor(query => query.Rooms)
            .GreaterThan(0);

        RuleFor(query => query.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinPrice.HasValue);

        RuleFor(query => query.MaxPrice)
            .GreaterThan(0)
            .When(query => query.MaxPrice.HasValue);

        RuleFor(query => query)
            .Must(query =>
                !query.MinPrice.HasValue ||
                !query.MaxPrice.HasValue ||
                query.MinPrice <= query.MaxPrice)
            .WithMessage("Minimum price cannot be greater than maximum price.");

        RuleFor(query => query.StarRating)
            .InclusiveBetween(1, 5)
            .When(query => query.StarRating.HasValue);

        RuleFor(query => query.Category)
            .IsInEnum()
            .When(query => query.Category.HasValue);

        RuleForEach(query => query.AmenityIds)
            .GreaterThan(0)
            .When(query => query.AmenityIds is not null);

        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 50);

        RuleFor(query => query.SortBy)
            .IsInEnum();
    }
}