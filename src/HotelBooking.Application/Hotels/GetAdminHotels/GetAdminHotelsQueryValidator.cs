using FluentValidation;

namespace HotelBooking.Application.Hotels.GetAdminHotels;

public sealed class GetAdminHotelsQueryValidator
    : AbstractValidator<GetAdminHotelsQuery>
{
    public GetAdminHotelsQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(200);
    }
}