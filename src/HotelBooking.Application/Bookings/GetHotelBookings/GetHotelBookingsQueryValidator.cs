using FluentValidation;

namespace HotelBooking.Application.Bookings.GetHotelBookings;

public sealed class GetHotelBookingsQueryValidator
    : AbstractValidator<GetHotelBookingsQuery>
{
    public GetHotelBookingsQueryValidator()
    {
        RuleFor(query => query.HotelId)
            .GreaterThan(0);

        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 50);
    }
}