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
    }
}