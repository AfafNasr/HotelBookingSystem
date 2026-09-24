using FluentValidation;

namespace HotelBooking.Application.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandValidator
    : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .GreaterThan(0);

        RuleFor(x => x.RoomNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.RoomType)
            .IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.AdultsCapacity)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.ChildrenCapacity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0);
    }
}