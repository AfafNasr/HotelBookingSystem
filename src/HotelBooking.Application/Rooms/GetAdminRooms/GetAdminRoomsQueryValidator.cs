using FluentValidation;

namespace HotelBooking.Application.Rooms.GetAdminRooms;

public sealed class GetAdminRoomsQueryValidator
    : AbstractValidator<GetAdminRoomsQuery>
{
    public GetAdminRoomsQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(20);
    }
}