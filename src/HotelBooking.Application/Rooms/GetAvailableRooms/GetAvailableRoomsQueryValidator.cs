using FluentValidation;

namespace HotelBooking.Application.Rooms.GetAvailableRooms;

public sealed class GetAvailableRoomsQueryValidator
    : AbstractValidator<GetAvailableRoomsQuery>
{
    public GetAvailableRoomsQueryValidator()
    {
        RuleFor(x => x.HotelId)
            .GreaterThan(0);

        RuleFor(x => x.RoomType)
            .IsInEnum();

        RuleFor(x => x.CheckInDate)
            .NotEmpty();

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate);

        RuleFor(x => x.Adults)
            .GreaterThan(0);

        RuleFor(x => x.Children)
            .GreaterThanOrEqualTo(0);
    }
}