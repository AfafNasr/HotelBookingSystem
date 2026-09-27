using FluentValidation;

namespace HotelBooking.Application.Hotels.GetNearbyAttractions;

public sealed class GetNearbyAttractionsQueryValidator
    : AbstractValidator<GetNearbyAttractionsQuery>
{
    public GetNearbyAttractionsQueryValidator()
    {
        RuleFor(query => query.HotelId)
            .GreaterThan(0);

        RuleFor(query => query.RadiusMeters)
            .InclusiveBetween(100, 10_000);

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, 20);
    }
}