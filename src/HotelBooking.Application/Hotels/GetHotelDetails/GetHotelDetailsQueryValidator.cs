using FluentValidation;

namespace HotelBooking.Application.Hotels.GetHotelDetails;

public sealed class GetHotelDetailsQueryValidator
    : AbstractValidator<GetHotelDetailsQuery>
{
    public GetHotelDetailsQueryValidator()
    {
        RuleFor(x => x.HotelId)
            .GreaterThan(0);

        RuleFor(x => x.CheckInDate)
            .NotEmpty();

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate);

        RuleFor(x => x.Adults)
            .GreaterThan(0);

        RuleFor(x => x.Children)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Rooms)
            .GreaterThan(0);
    }
}