using FluentValidation;

namespace HotelBooking.Application.Cities.GetAdminCities;

public sealed class GetAdminCitiesQueryValidator
    : AbstractValidator<GetAdminCitiesQuery>
{
    public GetAdminCitiesQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(200);
    }
}